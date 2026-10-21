using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProspeccaoLeads.Application.Common;
using ProspeccaoLeads.Application.DTOs.Auth;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Interfaces;

namespace ProspeccaoLeads.Infrastructure.Auth;

public class SupabaseAuthService : IAuthService
{
    private const string LocalAccessToken = "local-session";
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(2);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionStore _sessionStore;
    private readonly ILogger<SupabaseAuthService> _logger;
    private UserSessionDto? _currentSession;

    public SupabaseAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        IUserRepository userRepository,
        IUserSessionStore sessionStore,
        ILogger<SupabaseAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _userRepository = userRepository;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<Result<UserSessionDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<UserSessionDto>.Failure("E-mail e senha são obrigatórios.");
        }

        if (IsLocalMode())
        {
            return await LoginLocalAsync(dto, ct);
        }

        if (!TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            return AuthenticationNotConfigured<UserSessionDto>();
        }

        try
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                $"{baseUrl}/auth/v1/token?grant_type=password",
                anonKey,
                new { email = dto.Email.Trim(), password = dto.Password });
            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return Result<UserSessionDto>.Failure(MapAuthError(await ReadErrorAsync(response, ct)));
            }

            return await CreateAndPersistSessionAsync(
                await ReadJsonAsync(response, ct),
                dto.Email.Trim(),
                null,
                ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Erro ao conectar com Supabase Auth durante o login.");
            return Result<UserSessionDto>.Failure(GetConnectionFailureMessage(ex));
        }
    }

    public async Task<Result<UserSessionDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password) ||
            string.IsNullOrWhiteSpace(dto.Name))
        {
            return Result<UserSessionDto>.Failure("Preencha todos os campos obrigatórios.");
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            return Result<UserSessionDto>.Failure("A confirmação de senha não confere.");
        }

        if (IsLocalMode())
        {
            return Result<UserSessionDto>.Failure(
                "O cadastro está desativado no modo local. Use o usuário configurado no arquivo .env.");
        }

        if (!TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            return AuthenticationNotConfigured<UserSessionDto>();
        }

        try
        {
            var endpoint = $"{baseUrl}/auth/v1/signup";
            if (IsValidRedirectUrl(dto.RedirectUrl))
            {
                endpoint += $"?redirect_to={Uri.EscapeDataString(dto.RedirectUrl!)}";
            }

            using var request = CreateRequest(
                HttpMethod.Post,
                endpoint,
                anonKey,
                new
                {
                    email = dto.Email.Trim(),
                    password = dto.Password,
                    data = new { name = dto.Name.Trim() }
                });
            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return Result<UserSessionDto>.Failure(MapAuthError(await ReadErrorAsync(response, ct)));
            }

            var node = await ReadJsonAsync(response, ct);
            var accessToken = node?["access_token"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                return await CreateAndPersistSessionAsync(node, dto.Email.Trim(), dto.Name.Trim(), ct);
            }

            var userNode = node?["user"] ?? node;
            var userId = ParseUserId(userNode);
            if (userId == null)
            {
                return Result<UserSessionDto>.Failure("O Supabase não retornou um usuário válido para o cadastro.");
            }

            return Result<UserSessionDto>.Success(new UserSessionDto
            {
                UserId = userId.Value,
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim()
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Erro ao registrar usuário no Supabase.");
            return Result<UserSessionDto>.Failure(GetConnectionFailureMessage(ex));
        }
    }

    public Result<string> GetGoogleSignInUrl(string redirectUrl, string codeChallenge)
    {
        if (IsLocalMode())
        {
            return Result<string>.Failure("O login com Google não está disponível no modo local.");
        }

        if (!_configuration.GetValue("Authentication:GoogleEnabled", true))
        {
            return Result<string>.Failure("O login com Google ainda não foi habilitado nesta instalação.");
        }

        if (!TryGetSupabaseConfiguration(out var baseUrl, out _))
        {
            return AuthenticationNotConfigured<string>();
        }

        if (!IsValidRedirectUrl(redirectUrl) || string.IsNullOrWhiteSpace(codeChallenge))
        {
            return Result<string>.Failure("Não foi possível iniciar o login com Google: redirecionamento inválido.");
        }

        var url = $"{baseUrl}/auth/v1/authorize" +
                  "?provider=google" +
                  $"&redirect_to={Uri.EscapeDataString(redirectUrl)}" +
                  $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                  "&code_challenge_method=s256" +
                  $"&scopes={Uri.EscapeDataString("openid email profile")}";

        return Result<string>.Success(url);
    }

    public async Task<Result<UserSessionDto>> ExchangeOAuthCodeAsync(
        string authCode,
        string codeVerifier,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authCode) || string.IsNullOrWhiteSpace(codeVerifier))
        {
            return Result<UserSessionDto>.Failure("O retorno do login com Google está incompleto. Inicie o acesso novamente.");
        }

        if (!TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            return AuthenticationNotConfigured<UserSessionDto>();
        }

        try
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                $"{baseUrl}/auth/v1/token?grant_type=pkce",
                anonKey,
                new { auth_code = authCode, code_verifier = codeVerifier });
            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao trocar código PKCE do Supabase: {Error}", await ReadErrorAsync(response, ct));
                return Result<UserSessionDto>.Failure("O login com Google expirou ou já foi utilizado. Tente entrar novamente.");
            }

            return await CreateAndPersistSessionAsync(await ReadJsonAsync(response, ct), null, null, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Erro ao concluir login OAuth no Supabase.");
            return Result<UserSessionDto>.Failure(GetConnectionFailureMessage(ex));
        }
    }

    public async Task<Result<UserSessionDto>> CompleteTokenSignInAsync(
        string accessToken,
        string? refreshToken = null,
        int? expiresInSeconds = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result<UserSessionDto>.Failure("Token de autenticação não encontrado.");
        }

        if (!TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            return AuthenticationNotConfigured<UserSessionDto>();
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"{baseUrl}/auth/v1/user", anonKey, accessToken: accessToken);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return Result<UserSessionDto>.Failure("O link de autenticação é inválido ou expirou.");
            }

            return await CreateAndPersistSessionFromUserAsync(
                await ReadJsonAsync(response, ct),
                accessToken,
                refreshToken,
                expiresInSeconds,
                null,
                null,
                ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Erro ao validar token retornado pelo Supabase.");
            return Result<UserSessionDto>.Failure(GetConnectionFailureMessage(ex));
        }
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return Result.Failure("O e-mail é obrigatório.");
        }

        if (IsLocalMode())
        {
            return Result.Failure("No modo local, altere a senha em LocalAuth__Password no arquivo .env.");
        }

        if (!TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            return Result.Failure("Autenticação não configurada.");
        }

        try
        {
            var redirectUrl = IsValidRedirectUrl(dto.RedirectUrl)
                ? dto.RedirectUrl!
                : BuildConfiguredRedirectUrl("redefinir-senha");
            var endpoint = $"{baseUrl}/auth/v1/recover?redirect_to={Uri.EscapeDataString(redirectUrl)}";
            using var request = CreateRequest(
                HttpMethod.Post,
                endpoint,
                anonKey,
                new { email = dto.Email.Trim() });
            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response, ct);
                _logger.LogWarning("Supabase recover falhou: {Error}", error);
                return Result.Failure(MapAuthError(error));
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Erro ao solicitar recuperação de senha.");
            return Result.Failure(GetConnectionFailureMessage(ex));
        }
    }

    public async Task<Result> UpdatePasswordWithTokenAsync(
        string accessToken,
        string newPassword,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return Result.Failure("A nova senha deve ter no mínimo 6 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result.Failure("Token de redefinição não encontrado.");
        }

        return await UpdateSupabasePasswordAsync(accessToken, newPassword, ct);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
        {
            return Result.Failure("A nova senha deve ter no mínimo 6 caracteres.");
        }

        if (dto.NewPassword != dto.ConfirmNewPassword)
        {
            return Result.Failure("A confirmação da nova senha não confere.");
        }

        if (IsLocalMode())
        {
            return Result.Failure("No modo local, altere a senha em LocalAuth__Password no arquivo .env.");
        }

        var session = await GetCurrentUserAsync(ct);
        if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            return Result.Failure("Sessão de autenticação inválida ou expirada.");
        }

        return await UpdateSupabasePasswordAsync(session.AccessToken, dto.NewPassword, ct);
    }

    public async Task<Result<UserSessionDto>> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 2)
        {
            return Result<UserSessionDto>.Failure("O nome deve ter no mínimo 2 caracteres.");
        }

        var session = await GetCurrentUserAsync(ct);
        if (session == null)
        {
            return Result<UserSessionDto>.Failure("Sessão de autenticação inválida ou expirada.");
        }

        var trimmedName = dto.Name.Trim();
        if (!IsLocalSession(session) && TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            try
            {
                using var request = CreateRequest(
                    HttpMethod.Put,
                    $"{baseUrl}/auth/v1/user",
                    anonKey,
                    new { data = new { name = trimmedName } },
                    session.AccessToken);
                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return Result<UserSessionDto>.Failure("Não foi possível atualizar o perfil no serviço de autenticação.");
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Erro ao sincronizar perfil com Supabase Auth.");
                return Result<UserSessionDto>.Failure(GetConnectionFailureMessage(ex));
            }
        }

        session.Name = trimmedName;
        await UpsertUserAsync(session, ct);
        await SetCurrentSessionAsync(session, ct);
        return Result<UserSessionDto>.Success(session);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var session = _currentSession ?? await _sessionStore.LoadAsync(ct);
        try
        {
            if (session != null &&
                !IsLocalSession(session) &&
                !string.IsNullOrWhiteSpace(session.AccessToken) &&
                TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
            {
                using var request = CreateRequest(
                    HttpMethod.Post,
                    $"{baseUrl}/auth/v1/logout?scope=local",
                    anonKey,
                    accessToken: session.AccessToken);
                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("O Supabase recusou a revogação da sessão com status {StatusCode}.", response.StatusCode);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Não foi possível revogar a sessão remota; a sessão local será removida.");
        }
        finally
        {
            _currentSession = null;
            await _sessionStore.ClearAsync(ct);
        }
    }

    public async Task<UserSessionDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        _currentSession ??= await _sessionStore.LoadAsync(ct);
        if (_currentSession == null)
        {
            return null;
        }

        if (IsLocalSession(_currentSession))
        {
            if (_currentSession.ExpiresAt <= DateTime.UtcNow)
            {
                await ClearLocalSessionAsync(ct);
            }

            return _currentSession;
        }

        var jwtExpiration = GetJwtExpiration(_currentSession.AccessToken);
        if (jwtExpiration.HasValue &&
            (!_currentSession.ExpiresAt.HasValue || jwtExpiration.Value < _currentSession.ExpiresAt.Value))
        {
            _currentSession.ExpiresAt = jwtExpiration;
        }

        if (_currentSession.ExpiresAt > DateTime.UtcNow.Add(RefreshWindow))
        {
            return _currentSession;
        }

        if (!string.IsNullOrWhiteSpace(_currentSession.RefreshToken))
        {
            var refreshed = await RefreshSessionAsync(_currentSession, ct);
            if (refreshed != null)
            {
                return refreshed;
            }
        }

        await ClearLocalSessionAsync(ct);
        return null;
    }

    private async Task<Result> UpdateSupabasePasswordAsync(string accessToken, string newPassword, CancellationToken ct)
    {
        if (!TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            return Result.Failure("Autenticação não configurada.");
        }

        try
        {
            using var request = CreateRequest(
                HttpMethod.Put,
                $"{baseUrl}/auth/v1/user",
                anonKey,
                new { password = newPassword },
                accessToken);
            using var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            _logger.LogWarning("Falha ao atualizar senha no Supabase: {Error}", await ReadErrorAsync(response, ct));
            return Result.Failure("Não foi possível atualizar a senha. O link ou a sessão pode ter expirado.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Erro ao atualizar senha no Supabase.");
            return Result.Failure(GetConnectionFailureMessage(ex));
        }
    }

    private async Task<UserSessionDto?> RefreshSessionAsync(UserSessionDto current, CancellationToken ct)
    {
        if (!TryGetSupabaseConfiguration(out var baseUrl, out var anonKey))
        {
            return null;
        }

        try
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                $"{baseUrl}/auth/v1/token?grant_type=refresh_token",
                anonKey,
                new { refresh_token = current.RefreshToken });
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Refresh da sessão Supabase falhou com status {StatusCode}.", response.StatusCode);
                return null;
            }

            var result = await CreateAndPersistSessionAsync(
                await ReadJsonAsync(response, ct),
                current.Email,
                current.Name,
                ct);
            return result.IsSuccess ? result.Value : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Não foi possível renovar a sessão do Supabase.");
            return null;
        }
    }

    private async Task<Result<UserSessionDto>> CreateAndPersistSessionAsync(
        JsonNode? node,
        string? fallbackEmail,
        string? fallbackName,
        CancellationToken ct)
    {
        var accessToken = node?["access_token"]?.GetValue<string>();
        var userNode = node?["user"];
        if (string.IsNullOrWhiteSpace(accessToken) || userNode == null)
        {
            return Result<UserSessionDto>.Failure("O serviço de autenticação retornou uma sessão incompleta.");
        }

        return await CreateAndPersistSessionFromUserAsync(
            userNode,
            accessToken,
            node?["refresh_token"]?.GetValue<string>(),
            ReadInt(node?["expires_in"]),
            fallbackEmail,
            fallbackName,
            ct);
    }

    private async Task<Result<UserSessionDto>> CreateAndPersistSessionFromUserAsync(
        JsonNode? userNode,
        string accessToken,
        string? refreshToken,
        int? expiresInSeconds,
        string? fallbackEmail,
        string? fallbackName,
        CancellationToken ct)
    {
        var userId = ParseUserId(userNode);
        if (userId == null)
        {
            return Result<UserSessionDto>.Failure("O serviço de autenticação retornou um usuário inválido.");
        }

        var email = userNode?["email"]?.GetValue<string>() ?? fallbackEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<UserSessionDto>.Failure("O serviço de autenticação não retornou o e-mail do usuário.");
        }

        var metadata = userNode?["user_metadata"];
        var name = metadata?["name"]?.GetValue<string>()
                   ?? metadata?["full_name"]?.GetValue<string>()
                   ?? fallbackName
                   ?? email.Split('@')[0];
        var expiresAt = GetJwtExpiration(accessToken)
                        ?? DateTime.UtcNow.AddSeconds(Math.Max(60, expiresInSeconds ?? 3600));

        var session = new UserSessionDto
        {
            UserId = userId.Value,
            Name = name,
            Email = email,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        await UpsertUserAsync(session, ct);
        await SetCurrentSessionAsync(session, ct);
        return Result<UserSessionDto>.Success(session);
    }

    private async Task<Result<UserSessionDto>> LoginLocalAsync(LoginDto dto, CancellationToken ct)
    {
        var configuredEmail = _configuration["LocalAuth:Email"]?.Trim();
        var configuredPassword = _configuration["LocalAuth:Password"];
        var configuredName = _configuration["LocalAuth:Name"]?.Trim();

        if (string.IsNullOrWhiteSpace(configuredEmail) || string.IsNullOrWhiteSpace(configuredPassword))
        {
            return Result<UserSessionDto>.Failure(
                "Credenciais locais não configuradas. Defina LocalAuth__Email e LocalAuth__Password.");
        }

        var emailMatches = string.Equals(dto.Email.Trim(), configuredEmail, StringComparison.OrdinalIgnoreCase);
        var suppliedPassword = Encoding.UTF8.GetBytes(dto.Password);
        var expectedPassword = Encoding.UTF8.GetBytes(configuredPassword);
        var passwordMatches = suppliedPassword.Length == expectedPassword.Length &&
                              CryptographicOperations.FixedTimeEquals(suppliedPassword, expectedPassword);

        if (!emailMatches || !passwordMatches)
        {
            return Result<UserSessionDto>.Failure("Credenciais inválidas. Verifique seu e-mail e senha.");
        }

        var userId = Guid.TryParse(_configuration["LocalAuth:UserId"], out var configuredUserId)
            ? configuredUserId
            : Guid.Parse("11111111-1111-1111-1111-111111111111");
        var session = new UserSessionDto
        {
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(configuredName) ? configuredEmail.Split('@')[0] : configuredName,
            Email = configuredEmail,
            AccessToken = LocalAccessToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await UpsertUserAsync(session, ct);
        await SetCurrentSessionAsync(session, ct);
        return Result<UserSessionDto>.Success(session);
    }

    private async Task UpsertUserAsync(UserSessionDto session, CancellationToken ct)
    {
        await _userRepository.AddOrUpdateAsync(new UserProfile
        {
            Id = session.UserId,
            Name = session.Name,
            Email = session.Email,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task SetCurrentSessionAsync(UserSessionDto session, CancellationToken ct)
    {
        _currentSession = session;
        await _sessionStore.SaveAsync(session, ct);
    }

    private async Task ClearLocalSessionAsync(CancellationToken ct)
    {
        _currentSession = null;
        await _sessionStore.ClearAsync(ct);
    }

    private bool IsLocalMode()
    {
        return string.Equals(_configuration["Authentication:Mode"], "Local", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLocalSession(UserSessionDto session)
    {
        return string.Equals(session.AccessToken, LocalAccessToken, StringComparison.Ordinal);
    }

    private bool TryGetSupabaseConfiguration(out string baseUrl, out string anonKey)
    {
        var configuredUrl = _configuration["Supabase:Url"];
        var configuredKey = _configuration["Supabase:AnonKey"];
        var isValid = !string.IsNullOrWhiteSpace(configuredUrl) &&
                      !string.IsNullOrWhiteSpace(configuredKey) &&
                      !configuredUrl.Contains("SEU_PROJETO", StringComparison.OrdinalIgnoreCase) &&
                      !configuredKey.Contains("SUA_CHAVE", StringComparison.OrdinalIgnoreCase) &&
                      Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) &&
                      uri.Scheme == Uri.UriSchemeHttps;

        baseUrl = isValid ? configuredUrl!.Replace("/rest/v1", "").TrimEnd('/') : string.Empty;
        anonKey = isValid ? configuredKey! : string.Empty;
        return isValid;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string endpoint,
        string anonKey,
        object? payload = null,
        string? accessToken = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Add("apikey", anonKey);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (payload != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private string BuildConfiguredRedirectUrl(string path)
    {
        var baseUrl = (_configuration["App:BaseUrl"] ?? "http://localhost:5210").TrimEnd('/');
        return $"{baseUrl}/{path.TrimStart('/')}";
    }

    private static bool IsValidRedirectUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private static Guid? ParseUserId(JsonNode? userNode)
    {
        return Guid.TryParse(userNode?["id"]?.GetValue<string>(), out var userId) ? userId : null;
    }

    private static int? ReadInt(JsonNode? value)
    {
        if (value == null)
        {
            return null;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static DateTime? GetJwtExpiration(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            return jwt.ValidTo == DateTime.MinValue ? null : jwt.ValidTo.ToUniversalTime();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static async Task<JsonNode?> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonNode.Parse(json);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            var node = JsonNode.Parse(body);
            return node?["msg"]?.GetValue<string>()
                   ?? node?["error_description"]?.GetValue<string>()
                   ?? node?["message"]?.GetValue<string>()
                   ?? node?["error"]?.GetValue<string>()
                   ?? body;
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static string MapAuthError(string error)
    {
        if (error.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return "E-mail ainda não confirmado. Abra o link enviado pelo Supabase e tente novamente.";
        }

        if (error.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            return "Credenciais inválidas. Verifique seu e-mail e senha ou redefina a senha.";
        }

        if (error.Contains("User already registered", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("already been registered", StringComparison.OrdinalIgnoreCase))
        {
            return "Este e-mail já está cadastrado. Faça login ou solicite a recuperação de senha.";
        }

        if (error.Contains("provider is not enabled", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("Unsupported provider", StringComparison.OrdinalIgnoreCase))
        {
            return "O login com Google ainda não foi habilitado no painel do Supabase.";
        }

        if (error.Contains("signup is disabled", StringComparison.OrdinalIgnoreCase))
        {
            return "Novos cadastros estão desabilitados no Supabase.";
        }

        if (error.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase))
        {
            return "O limite temporário de envio de e-mails foi atingido. Aguarde alguns minutos e tente novamente.";
        }

        return "Não foi possível concluir a autenticação. Tente novamente ou use a recuperação de senha.";
    }

    private static string GetConnectionFailureMessage(Exception exception)
    {
        if (exception.GetBaseException() is SocketException { SocketErrorCode: SocketError.HostNotFound })
        {
            return "Não foi possível localizar o projeto Supabase configurado. Verifique se o projeto existe, está ativo e se Supabase__Url está correta.";
        }

        if (exception is TaskCanceledException)
        {
            return "O serviço de autenticação demorou demais para responder. Tente novamente em instantes.";
        }

        return "Não foi possível conectar ao serviço de autenticação. Tente novamente em instantes.";
    }

    private static Result<T> AuthenticationNotConfigured<T>()
    {
        return Result<T>.Failure(
            "Autenticação não configurada. Defina Authentication__Mode=Local ou configure o Supabase.");
    }
}
