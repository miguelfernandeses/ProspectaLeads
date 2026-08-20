using FluentAssertions;
using Moq;
using ProspeccaoLeads.Application.Services;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Domain.Interfaces;
using Xunit;

namespace ProspeccaoLeads.Tests.UnitTests.Services;

public class DashboardServiceTests
{
    private readonly Mock<ILeadRepository> _leadRepoMock;
    private readonly Mock<ISearchHistoryRepository> _historyRepoMock;
    private readonly DashboardService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        _leadRepoMock = new Mock<ILeadRepository>();
        _historyRepoMock = new Mock<ISearchHistoryRepository>();
        _service = new DashboardService(_leadRepoMock.Object, _historyRepoMock.Object);
    }

    [Fact]
    public async Task ObterResumoAsync_DeveCalcularTaxaDeConversaoCorretamente()
    {
        // Arrange: 10 leads no total, 2 clientes conquistados -> Conversão = 20.0%
        var leads = new List<Lead>
        {
            new(_userId, "L1", status: StatusLead.Novo),
            new(_userId, "L2", status: StatusLead.Contatado),
            new(_userId, "L3", status: StatusLead.EmNegociacao),
            new(_userId, "L4", status: StatusLead.Cliente),
            new(_userId, "L5", status: StatusLead.Cliente),
            new(_userId, "L6", status: StatusLead.Interessado),
            new(_userId, "L7", status: StatusLead.Novo),
            new(_userId, "L8", status: StatusLead.Novo),
            new(_userId, "L9", status: StatusLead.SemInteresse),
            new(_userId, "L10", status: StatusLead.Novo)
        };

        var history = new List<SearchHistory>
        {
            new() { Id = Guid.NewGuid(), UserId = _userId, Niche = "Dentistas", Location = "SP", ResultCount = 50 }
        };

        _leadRepoMock.Setup(r => r.GetAllAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(leads);
        _historyRepoMock.Setup(r => r.GetByUserIdAsync(_userId, 500, It.IsAny<CancellationToken>())).ReturnsAsync(history);

        // Act
        var result = await _service.ObterResumoAsync(_userId);

        // Assert
        result.TotalEncontrados.Should().Be(50);
        result.TotalSalvos.Should().Be(10);
        result.ClientesConquistados.Should().Be(2);
        result.TaxaConversao.Should().Be(20.0m);
        result.Contatados.Should().Be(1);
        result.EmNegociacao.Should().Be(1);
    }

    [Fact]
    public async Task ObterResumoAsync_SemLeads_DeveRetornarTaxaZero()
    {
        // Arrange
        _leadRepoMock.Setup(r => r.GetAllAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Lead>());
        _historyRepoMock.Setup(r => r.GetByUserIdAsync(_userId, 500, It.IsAny<CancellationToken>())).ReturnsAsync(new List<SearchHistory>());

        // Act
        var result = await _service.ObterResumoAsync(_userId);

        // Assert
        result.TotalSalvos.Should().Be(0);
        result.ClientesConquistados.Should().Be(0);
        result.TaxaConversao.Should().Be(0m);
    }
}
