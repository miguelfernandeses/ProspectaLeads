using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ProspeccaoLeads.Application.DTOs.Auth;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Interfaces;
using ProspeccaoLeads.Infrastructure.Auth;

namespace ProspeccaoLeads.Tests.UnitTests.Services;

public class SupabaseAuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserSessionStore> _sessionStore = new();
    private readonly Mock<ILogger<SupabaseAuthService>> _logger = new();

    [Fact]
    public async Task LoginLocal_ComSenhaIncorreta_DeveFalhar()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "Local",
            ["LocalAuth:Email"] = "admin@local.test",
            ["LocalAuth:Password"] = "senha-correta",
            ["LocalAuth:Name"] = "Administrador"
        });

        var result = await service.LoginAsync(new LoginDto
        {
            Email = "admin@local.test",
            Password = "senha-incorreta"
        });

        result.IsFailure.Should().BeTrue();
        _userRepository.Verify(
            repository => repository.AddOrUpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginSemProvedorConfigurado_DeveFalhar()
    {
        var service = CreateService(new Dictionary<string, string?>());

        var result = await service.LoginAsync(new LoginDto
        {
            Email = "qualquer@local.test",
            Password = "qualquer-senha"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task LoginLocal_ComCredenciaisCorretas_DeveCriarSessaoUnica()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "Local",
            ["LocalAuth:Email"] = "admin@local.test",
            ["LocalAuth:Password"] = "senha-correta",
            ["LocalAuth:Name"] = "Meu ProspectAI"
        });

        var result = await service.LoginAsync(new LoginDto
        {
            Email = "admin@local.test",
            Password = "senha-correta"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Meu ProspectAI");
        result.Value.Email.Should().Be("admin@local.test");
        _sessionStore.Verify(
            store => store.SaveAsync(result.Value, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ComSessaoPersistidaValida_DeveRestaurarSessao()
    {
        var persistedSession = new UserSessionDto
        {
            UserId = Guid.NewGuid(),
            Name = "Administrador",
            Email = "admin@local.test",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _sessionStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedSession);
        var service = CreateService(new Dictionary<string, string?>());

        var session = await service.GetCurrentUserAsync();

        session.Should().BeSameAs(persistedSession);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ComSessaoExpirada_DeveRemoverSessao()
    {
        _sessionStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSessionDto { ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });
        var service = CreateService(new Dictionary<string, string?>());

        var session = await service.GetCurrentUserAsync();

        session.Should().BeNull();
        _sessionStore.Verify(
            store => store.ClearAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ComConfirmacaoDeEmail_DeveNaoPersistirSessaoSemToken()
    {
        var userId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, $$"""
            {
              "id": "{{userId}}",
              "email": "amigo@example.com",
              "user_metadata": { "name": "Amigo" }
            }
            """));
        var service = CreateService(CreateSupabaseSettings(), handler);

        var result = await service.RegisterAsync(new RegisterDto
        {
            Name = "Amigo",
            Email = "amigo@example.com",
            Password = "senha-segura",
            ConfirmPassword = "senha-segura",
            RedirectUrl = "http://localhost:5210/auth/callback?flow=signup"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().BeNull();
        _sessionStore.Verify(store => store.SaveAsync(It.IsAny<UserSessionDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(repository => repository.AddOrUpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        handler.LastRequestUri.Should().Contain("redirect_to=");
    }

    [Fact]
    public async Task LoginAsync_ComSupabase_DevePersistirRefreshTokenEExpiracaoReal()
    {
        var userId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, $$"""
            {
              "access_token": "access-token",
              "refresh_token": "refresh-token",
              "expires_in": 3600,
              "user": {
                "id": "{{userId}}",
                "email": "amigo@example.com",
                "user_metadata": { "name": "Amigo" }
              }
            }
            """));
        var service = CreateService(CreateSupabaseSettings(), handler);

        var before = DateTime.UtcNow;
        var result = await service.LoginAsync(new LoginDto
        {
            Email = "amigo@example.com",
            Password = "senha-segura"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresAt.Should().BeAfter(before.AddMinutes(59));
        _sessionStore.Verify(store => store.SaveAsync(result.Value, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ComTokenExpiradoERefreshToken_DeveRenovarSessao()
    {
        var userId = Guid.NewGuid();
        _sessionStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSessionDto
            {
                UserId = userId,
                Name = "Amigo",
                Email = "amigo@example.com",
                AccessToken = "expired-token",
                RefreshToken = "old-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, $$"""
            {
              "access_token": "renewed-access-token",
              "refresh_token": "renewed-refresh-token",
              "expires_in": 3600,
              "user": {
                "id": "{{userId}}",
                "email": "amigo@example.com",
                "user_metadata": { "name": "Amigo" }
              }
            }
            """));
        var service = CreateService(CreateSupabaseSettings(), handler);

        var session = await service.GetCurrentUserAsync();

        session.Should().NotBeNull();
        session!.AccessToken.Should().Be("renewed-access-token");
        session.RefreshToken.Should().Be("renewed-refresh-token");
        handler.LastRequestUri.Should().Contain("grant_type=refresh_token");
    }

    [Fact]
    public void GetGoogleSignInUrl_DeveUsarPkceERedirectSeguro()
    {
        var service = CreateService(CreateSupabaseSettings());

        var result = service.GetGoogleSignInUrl(
            "http://localhost:5210/auth/callback",
            "challenge-seguro");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("provider=google");
        result.Value.Should().Contain("code_challenge=challenge-seguro");
        result.Value.Should().Contain("code_challenge_method=s256");
        result.Value.Should().Contain(Uri.EscapeDataString("http://localhost:5210/auth/callback"));
    }

    [Fact]
    public async Task LogoutAsync_DeveRevogarERemoverSessaoPersistida()
    {
        _sessionStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSessionDto
            {
                UserId = Guid.NewGuid(),
                Name = "Amigo",
                Email = "amigo@example.com",
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var service = CreateService(CreateSupabaseSettings(), handler);

        await service.LogoutAsync();

        handler.LastRequestUri.Should().Contain("/auth/v1/logout");
        _sessionStore.Verify(store => store.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ComHostSupabaseInexistente_DeveOrientarRevisaoDoProjeto()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new HttpRequestException(
                "Host not found",
                new SocketException((int)SocketError.HostNotFound)));
        var service = CreateService(CreateSupabaseSettings(), handler);

        var result = await service.LoginAsync(new LoginDto
        {
            Email = "amigo@example.com",
            Password = "senha-segura"
        });

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("projeto Supabase");
        result.ErrorMessage.Should().Contain("Supabase__Url");
    }

    private SupabaseAuthService CreateService(
        Dictionary<string, string?> values,
        HttpMessageHandler? handler = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new SupabaseAuthService(
            handler == null ? new HttpClient() : new HttpClient(handler),
            configuration,
            _userRepository.Object,
            _sessionStore.Object,
            _logger.Object);
    }

    private static Dictionary<string, string?> CreateSupabaseSettings()
    {
        return new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "Supabase",
            ["Authentication:GoogleEnabled"] = "true",
            ["Supabase:Url"] = "https://project.supabase.co",
            ["Supabase:AnonKey"] = "public-anon-key"
        };
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public string? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            return Task.FromResult(_responseFactory(request));
        }
    }
}
