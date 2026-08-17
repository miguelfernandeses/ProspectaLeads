using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SupabaseAuthService> _logger;

    private static UserSessionDto? _currentSession;

    public SupabaseAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        IUserRepository userRepository,
        ILogger<SupabaseAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserSessionDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<UserSessionDto>.Failure("E-mail e senha são obrigatórios.");
        }

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        // Se o Supabase estiver configurado com chaves reais
        if (!string.IsNullOrWhiteSpace(supabaseUrl) &&
            !string.IsNullOrWhiteSpace(supabaseKey) &&
            !supabaseUrl.Contains("SEU_PROJETO") &&
            !supabaseKey.Contains("SUA_CHAVE"))
        {
            try
            {
                var baseUrl = supabaseUrl.Replace("/rest/v1", "").TrimEnd('/');
                var endpoint = $"{baseUrl}/auth/v1/token?grant_type=password";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("apikey", supabaseKey);

                var payload = JsonSerializer.Serialize(new
                {
                    email = dto.Email.Trim(),
                    password = dto.Password
                });

                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var node = JsonNode.Parse(json);
                    var accessToken = node?["access_token"]?.ToString();
                    var userNode = node?["user"];
                    var userIdStr = userNode?["id"]?.ToString();
                    var email = userNode?["email"]?.ToString() ?? dto.Email;
                    var name = userNode?["user_metadata"]?["name"]?.ToString() ?? email.Split('@')[0];

                    if (Guid.TryParse(userIdStr, out var userId))
                    {
                        var session = new UserSessionDto
                        {
                            UserId = userId,
                            Name = name,
                            Email = email,
                            AccessToken = accessToken,
                            ExpiresAt = DateTime.UtcNow.AddDays(7)
                        };

                        await _userRepository.AddOrUpdateAsync(new UserProfile
                        {
                            Id = userId,
                            Name = name,
                            Email = email,
                            CreatedAt = DateTime.UtcNow
                        }, ct);

                        _currentSession = session;
                        return Result<UserSessionDto>.Success(session);
                    }
                }
                else
                {
                    var errJson = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Supabase Auth falhou: {StatusCode} - {Err}", response.StatusCode, errJson);
                    
                    var errNode = JsonNode.Parse(errJson);
                    var msg = errNode?["msg"]?.ToString() ?? errNode?["error_description"]?.ToString() ?? errNode?["message"]?.ToString();

                    if (msg != null && msg.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<UserSessionDto>.Failure("E-mail não confirmado. Verifique o link enviado ao seu e-mail ou desative a confirmação de e-mail no painel do Supabase (Authentication -> Providers -> Email).");
                    }
                    if (msg != null && (msg.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase) || msg.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase)))
                    {
                        return Result<UserSessionDto>.Failure("Credenciais inválidas. Verifique seu e-mail e senha.");
                    }

                    return Result<UserSessionDto>.Failure(msg ?? "Credenciais inválidas. Verifique seu e-mail e senha.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com Supabase Auth.");
                return Result<UserSessionDto>.Failure("Erro ao conectar com servidor de autenticação.");
            }
        }

        // Modo Demonstração / Desenvolvimento Local (Zero setup friction)
        var demoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var demoName = dto.Email.Split('@')[0];
        if (string.Equals(dto.Email, "admin@prospeccaoleads.com", StringComparison.OrdinalIgnoreCase))
        {
            demoName = "Administrador";
        }

        var demoSession = new UserSessionDto
        {
            UserId = demoUserId,
            Name = char.ToUpper(demoName[0]) + demoName[1..],
            Email = dto.Email.Trim(),
            AccessToken = "local-dev-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _userRepository.AddOrUpdateAsync(new UserProfile
        {
            Id = demoUserId,
            Name = demoSession.Name,
            Email = demoSession.Email,
            CreatedAt = DateTime.UtcNow
        }, ct);

        _currentSession = demoSession;
        return Result<UserSessionDto>.Success(demoSession);
    }

    public async Task<Result<UserSessionDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return Result<UserSessionDto>.Failure("Preencha todos os campos obrigatórios.");
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            return Result<UserSessionDto>.Failure("A confirmação de senha não confere.");
        }

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        if (!string.IsNullOrWhiteSpace(supabaseUrl) &&
            !string.IsNullOrWhiteSpace(supabaseKey) &&
            !supabaseUrl.Contains("SEU_PROJETO") &&
            !supabaseKey.Contains("SUA_CHAVE"))
        {
            try
            {
                var baseUrl = supabaseUrl.Replace("/rest/v1", "").TrimEnd('/');
                var endpoint = $"{baseUrl}/auth/v1/signup";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("apikey", supabaseKey);

                var payload = JsonSerializer.Serialize(new
                {
                    email = dto.Email.Trim(),
                    password = dto.Password,
                    data = new { name = dto.Name.Trim() }
                });

                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var node = JsonNode.Parse(json);
                    var accessToken = node?["access_token"]?.ToString();
                    var userNode = node?["user"] ?? node;
                    var userIdStr = userNode?["id"]?.ToString();

                    if (Guid.TryParse(userIdStr, out var userId))
                    {
                        var session = new UserSessionDto
                        {
                            UserId = userId,
                            Name = dto.Name.Trim(),
                            Email = dto.Email.Trim(),
                            AccessToken = accessToken,
                            ExpiresAt = DateTime.UtcNow.AddDays(7)
                        };

                        await _userRepository.AddOrUpdateAsync(new UserProfile
                        {
                            Id = userId,
                            Name = session.Name,
                            Email = session.Email,
                            CreatedAt = DateTime.UtcNow
                        }, ct);

                        _currentSession = session;
                        return Result<UserSessionDto>.Success(session);
                    }
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Supabase Signup falhou: {Error}", errorJson);

                    var errNode = JsonNode.Parse(errorJson);
                    var msg = errNode?["msg"]?.ToString() ?? errNode?["error_description"]?.ToString() ?? errNode?["message"]?.ToString();

                    if (msg != null && msg.Contains("User already registered", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<UserSessionDto>.Failure("Este e-mail já está cadastrado no Supabase. Faça login ou solicite recuperação de senha.");
                    }
                    if (msg != null && (msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || msg.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase)))
                    {
                        return Result<UserSessionDto>.Failure("Limite de envio de e-mails do Supabase atingido. Para permitir cadastros imediatos, desative a opção 'Confirm email' no menu Authentication -> Providers -> Email do Supabase.");
                    }

                    return Result<UserSessionDto>.Failure(msg ?? "Não foi possível criar a conta no Supabase.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar usuário no Supabase.");
                return Result<UserSessionDto>.Failure("Erro ao conectar com servidor de cadastro.");
            }
        }

        // Modo Demonstração / Desenvolvimento Local
        var newUserId = Guid.NewGuid();
        var sessionCreated = new UserSessionDto
        {
            UserId = newUserId,
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            AccessToken = "local-dev-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _userRepository.AddOrUpdateAsync(new UserProfile
        {
            Id = newUserId,
            Name = sessionCreated.Name,
            Email = sessionCreated.Email,
            CreatedAt = DateTime.UtcNow
        }, ct);

        _currentSession = sessionCreated;
        return Result<UserSessionDto>.Success(sessionCreated);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return Result.Failure("O e-mail é obrigatório.");
        }

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        if (!string.IsNullOrWhiteSpace(supabaseUrl) &&
            !string.IsNullOrWhiteSpace(supabaseKey) &&
            !supabaseUrl.Contains("SEU_PROJETO") &&
            !supabaseKey.Contains("SUA_CHAVE"))
        {
            try
            {
                var baseUrl = supabaseUrl.Replace("/rest/v1", "").TrimEnd('/');
                var redirectUrl = "http://localhost:5000/redefinir-senha";
                var endpoint = $"{baseUrl}/auth/v1/recover?redirect_to={Uri.EscapeDataString(redirectUrl)}";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("apikey", supabaseKey);

                var payload = JsonSerializer.Serialize(new
                {
                    email = dto.Email.Trim(),
                    redirect_to = redirectUrl
                });
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Supabase recover falhou: {Err}", err);

                    var errNode = JsonNode.Parse(err);
                    var msg = errNode?["msg"]?.ToString() ?? errNode?["error_description"]?.ToString() ?? errNode?["message"]?.ToString();

                    if (msg != null && (msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || msg.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase)))
                    {
                        return Result.Failure("Limite de envio de e-mails do Supabase atingido (máx. 3-4 e-mails por hora no plano gratuito do Supabase). Aguarde alguns minutos ou desative a confirmação de e-mail / configure SMTP no painel do Supabase.");
                    }

                    return Result.Failure(msg ?? "Não foi possível enviar o e-mail de recuperação.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao solicitar recuperação de senha.");
                return Result.Failure("Erro ao conectar com servidor de autenticação.");
            }
        }

        return Result.Success();
    }

    public async Task<Result> UpdatePasswordWithTokenAsync(string accessToken, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return Result.Failure("A nova senha deve ter no mínimo 6 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result.Failure("Token de redefinição não encontrado.");
        }

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        if (!string.IsNullOrWhiteSpace(supabaseUrl) &&
            !string.IsNullOrWhiteSpace(supabaseKey) &&
            !supabaseUrl.Contains("SEU_PROJETO") &&
            !supabaseKey.Contains("SUA_CHAVE"))
        {
            try
            {
                var baseUrl = supabaseUrl.Replace("/rest/v1", "").TrimEnd('/');
                var endpoint = $"{baseUrl}/auth/v1/user";
                using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                request.Headers.Add("apikey", supabaseKey);
                request.Headers.Add("Authorization", $"Bearer {accessToken.Trim()}");

                var payload = JsonSerializer.Serialize(new { password = newPassword });
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    return Result.Success();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Falha ao redefinir senha com token: {Err}", err);
                    return Result.Failure("Não foi possível redefinir a senha. O link pode ter expirado.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com Supabase Auth.");
                return Result.Failure("Erro ao conectar com servidor de autenticação.");
            }
        }

        return Result.Success();
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

        var session = _currentSession;
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        if (session != null &&
            !string.IsNullOrWhiteSpace(session.AccessToken) &&
            session.AccessToken != "local-dev-token" &&
            !string.IsNullOrWhiteSpace(supabaseUrl) &&
            !string.IsNullOrWhiteSpace(supabaseKey) &&
            !supabaseUrl.Contains("SEU_PROJETO") &&
            !supabaseKey.Contains("SUA_CHAVE"))
        {
            try
            {
                var baseUrl = supabaseUrl.Replace("/rest/v1", "").TrimEnd('/');
                var endpoint = $"{baseUrl}/auth/v1/user";
                using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                request.Headers.Add("apikey", supabaseKey);
                request.Headers.Add("Authorization", $"Bearer {session.AccessToken}");

                var payload = JsonSerializer.Serialize(new { password = dto.NewPassword });
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    return Result.Success();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Falha ao alterar senha no Supabase: {Err}", err);
                    return Result.Failure("Não foi possível atualizar a senha no Supabase.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com Supabase Auth para troca de senha.");
                return Result.Failure("Erro ao conectar com o serviço de autenticação.");
            }
        }

        // Modo Demonstração / Sessão Local
        return Result.Success();
    }

    public async Task<Result<UserSessionDto>> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 2)
        {
            return Result<UserSessionDto>.Failure("O nome deve ter no mínimo 2 caracteres.");
        }

        var session = _currentSession ?? new UserSessionDto
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "admin@prospeccaoleads.com",
            Name = "Administrador"
        };

        var trimmedName = dto.Name.Trim();
        session.Name = trimmedName;

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        if (!string.IsNullOrWhiteSpace(session.AccessToken) &&
            session.AccessToken != "local-dev-token" &&
            !string.IsNullOrWhiteSpace(supabaseUrl) &&
            !string.IsNullOrWhiteSpace(supabaseKey) &&
            !supabaseUrl.Contains("SEU_PROJETO") &&
            !supabaseKey.Contains("SUA_CHAVE"))
        {
            try
            {
                var baseUrl = supabaseUrl.Replace("/rest/v1", "").TrimEnd('/');
                var endpoint = $"{baseUrl}/auth/v1/user";
                using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                request.Headers.Add("apikey", supabaseKey);
                request.Headers.Add("Authorization", $"Bearer {session.AccessToken}");

                var payload = JsonSerializer.Serialize(new
                {
                    data = new { name = trimmedName }
                });
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                await _httpClient.SendAsync(request, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Aviso ao sincronizar metadados do perfil com Supabase Auth.");
            }
        }

        // Salva na tabela user_profiles do banco de dados (Supabase PostgreSQL ou local)
        await _userRepository.AddOrUpdateAsync(new UserProfile
        {
            Id = session.UserId,
            Name = trimmedName,
            Email = session.Email,
            CreatedAt = DateTime.UtcNow
        }, ct);

        _currentSession = session;
        return Result<UserSessionDto>.Success(session);
    }

    public Task LogoutAsync(CancellationToken ct = default)
    {
        _currentSession = null;
        return Task.CompletedTask;
    }

    public Task<UserSessionDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        // Se ainda não houver sessão ativa, inicializa sessão padrão para visualização imediata
        if (_currentSession == null)
        {
            _currentSession = new UserSessionDto
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Demonstração",
                Email = "admin@prospeccaoleads.com",
                AccessToken = "local-dev-token",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
        }

        return Task.FromResult<UserSessionDto?>(_currentSession);
    }
}
