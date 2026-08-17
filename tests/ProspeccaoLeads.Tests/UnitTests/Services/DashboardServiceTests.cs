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
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L1", Status = StatusLead.Novo },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L2", Status = StatusLead.Contatado },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L3", Status = StatusLead.EmNegociacao },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L4", Status = StatusLead.Cliente },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L5", Status = StatusLead.Cliente },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L6", Status = StatusLead.Interessado },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L7", Status = StatusLead.Novo },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L8", Status = StatusLead.Novo },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L9", Status = StatusLead.SemInteresse },
            new() { Id = Guid.NewGuid(), UserId = _userId, Nome = "L10", Status = StatusLead.Novo }
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
