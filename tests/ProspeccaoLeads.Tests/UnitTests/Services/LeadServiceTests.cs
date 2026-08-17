using FluentAssertions;
using Moq;
using ProspeccaoLeads.Application.DTOs.Lead;
using ProspeccaoLeads.Application.Services;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Domain.Interfaces;
using Xunit;

namespace ProspeccaoLeads.Tests.UnitTests.Services;

public class LeadServiceTests
{
    private readonly Mock<ILeadRepository> _repoMock;
    private readonly LeadService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public LeadServiceTests()
    {
        _repoMock = new Mock<ILeadRepository>();
        _service = new LeadService(_repoMock.Object);
    }

    [Fact]
    public async Task CriarAsync_ComDadosValidos_DeveSalvarComSucesso()
    {
        // Arrange
        var dto = new CreateLeadDto
        {
            UserId = _userId,
            Nome = "Clínica Dental Sorriso",
            Categoria = "Clínica odontológica",
            Cidade = "São Paulo",
            Estado = "SP",
            Telefone = "(11) 98888-7777",
            Status = StatusLead.Novo
        };

        _repoMock.Setup(r => r.ExistsByNameAndCityAsync(_userId, dto.Nome, dto.Cidade, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repoMock.Setup(r => r.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead l, CancellationToken _) => l);

        // Act
        var result = await _service.CriarAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Nome.Should().Be("Clínica Dental Sorriso");
        result.Value.Status.Should().Be(StatusLead.Novo);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_QuandoLeadJaExiste_DeveRetornarFalha()
    {
        // Arrange
        var dto = new CreateLeadDto
        {
            UserId = _userId,
            Nome = "Clínica Existente",
            Cidade = "São Paulo"
        };

        _repoMock.Setup(r => r.ExistsByNameAndCityAsync(_userId, dto.Nome, dto.Cidade, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CriarAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("já está cadastrado");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarStatusAsync_QuandoLeadExiste_DeveAlterarStatusComSucesso()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        var lead = new Lead
        {
            Id = leadId,
            UserId = _userId,
            Nome = "Academia Iron Fit",
            Status = StatusLead.Novo
        };

        _repoMock.Setup(r => r.GetByIdAsync(leadId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        // Act
        var result = await _service.AtualizarStatusAsync(leadId, StatusLead.Contatado, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(StatusLead.Contatado);
        _repoMock.Verify(r => r.UpdateAsync(lead, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirAsync_QuandoLeadNaoExiste_DeveRetornarFalha()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(leadId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead?)null);

        // Act
        var result = await _service.ExcluirAsync(leadId, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Lead não encontrado.");
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
