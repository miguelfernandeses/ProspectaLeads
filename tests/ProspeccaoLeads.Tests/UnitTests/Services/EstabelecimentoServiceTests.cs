using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Application.Services;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Interfaces;
using Xunit;

namespace ProspeccaoLeads.Tests.UnitTests.Services;

public class EstabelecimentoServiceTests
{
    private readonly Mock<IEstabelecimentoProvider> _providerMock;
    private readonly Mock<ILeadRepository> _leadRepoMock;
    private readonly Mock<ISearchHistoryService> _historyServiceMock;
    private readonly Mock<ILogger<EstabelecimentoService>> _loggerMock;
    private readonly EstabelecimentoService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public EstabelecimentoServiceTests()
    {
        _providerMock = new Mock<IEstabelecimentoProvider>();
        _leadRepoMock = new Mock<ILeadRepository>();
        _historyServiceMock = new Mock<ISearchHistoryService>();
        _loggerMock = new Mock<ILogger<EstabelecimentoService>>();

        _providerMock.Setup(p => p.Prioridade).Returns(1);
        _providerMock.Setup(p => p.NomeProvedor).Returns("MockProvider");
        _providerMock.Setup(p => p.DisponivelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _service = new EstabelecimentoService(
            new[] { _providerMock.Object },
            _leadRepoMock.Object,
            _historyServiceMock.Object,
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            _loggerMock.Object);
    }

    [Fact]
    public async Task BuscarAsync_QuandoEncontraResultados_DeveIdentificarLeadsJaSalvos()
    {
        // Arrange
        var estabelecimentos = new List<EstabelecimentoDto>
        {
            new() { Nome = "Academia Fit 1", Cidade = "São Paulo" },
            new() { Nome = "Academia Fit 2", Cidade = "São Paulo" }
        };

        var existingLeadId = Guid.NewGuid();
        var leadsSalvos = new List<Lead>
        {
            new() { Id = existingLeadId, UserId = _userId, Nome = "Academia Fit 1", Cidade = "São Paulo" }
        };

        _providerMock.Setup(p => p.BuscarAsync("Academia", "São Paulo - SP", 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estabelecimentos);

        _leadRepoMock.Setup(r => r.GetAllAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leadsSalvos);

        // Act
        var result = await _service.BuscarAsync(new BuscaEstabelecimentoDto
        {
            Nicho = "Academia",
            Localizacao = "São Paulo - SP"
        }, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var first = result.Value!.First(x => x.Nome == "Academia Fit 1");
        first.JaSalvoComoLead.Should().BeTrue();
        first.LeadId.Should().Be(existingLeadId);

        var second = result.Value!.First(x => x.Nome == "Academia Fit 2");
        second.JaSalvoComoLead.Should().BeFalse();

        _historyServiceMock.Verify(h => h.RegistrarBuscaAsync(_userId, "Academia", "São Paulo - SP", 2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
