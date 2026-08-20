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
        var stats = new ProspeccaoLeads.Domain.DTOs.DashboardLeadStats
        {
            TotalSalvos = 10,
            ClientesConquistados = 2,
            Contatados = 1,
            EmNegociacao = 1,
            NovosHoje = 3,
            LeadsPorNicho = new() { new() { Key = "Dentistas", Count = 5 } },
            LeadsPorCidade = new() { new() { Key = "São Paulo", Count = 10 } },
            LeadsPorStatus = new() { { StatusLead.Novo, 6 }, { StatusLead.Cliente, 2 } },
            EvolucaoMensal = new() { new() { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month, TotalCreated = 10, TotalConverted = 2 } }
        };

        var history = new List<SearchHistory>
        {
            new() { Id = Guid.NewGuid(), UserId = _userId, Niche = "Dentistas", Location = "SP", ResultCount = 50 }
        };

        _leadRepoMock.Setup(r => r.GetDashboardStatsAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(stats);
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
        _leadRepoMock.Setup(r => r.GetDashboardStatsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProspeccaoLeads.Domain.DTOs.DashboardLeadStats());
        _historyRepoMock.Setup(r => r.GetByUserIdAsync(_userId, 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchHistory>());

        // Act
        var result = await _service.ObterResumoAsync(_userId);

        // Assert
        result.TotalSalvos.Should().Be(0);
        result.ClientesConquistados.Should().Be(0);
        result.TaxaConversao.Should().Be(0m);
    }
}
