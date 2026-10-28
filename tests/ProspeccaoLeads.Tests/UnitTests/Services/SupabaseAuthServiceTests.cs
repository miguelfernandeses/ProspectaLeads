using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
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

    private SupabaseAuthService CreateService(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new SupabaseAuthService(
            new HttpClient(),
            configuration,
            _userRepository.Object,
            _sessionStore.Object,
            _logger.Object);
    }
}
