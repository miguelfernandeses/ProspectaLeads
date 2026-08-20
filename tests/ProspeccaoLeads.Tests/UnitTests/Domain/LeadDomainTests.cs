using FluentAssertions;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Domain.Exceptions;
using Xunit;

namespace ProspeccaoLeads.Tests.UnitTests.Domain;

public class LeadDomainTests
{
    [Fact]
    public void CriarLead_ComDadosValidos_DeveInstanciarComSucesso()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nome = "Clínica Saúde & Vida";
        var categoria = "Clínicas médicas";

        // Act
        var lead = new Lead(userId, nome, categoria);

        // Assert
        lead.Should().NotBeNull();
        lead.Id.Should().NotBeEmpty();
        lead.UserId.Should().Be(userId);
        lead.Nome.Should().Be(nome);
        lead.Categoria.Should().Be(categoria);
        lead.Status.Should().Be(StatusLead.Novo);
        lead.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        lead.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CriarLead_ComNomeInvalido_DeveLancarDomainException(string? nomeInvalido)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        Action act = () => new Lead(userId, nomeInvalido!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*nome do estabelecimento é obrigatório*");
    }

    [Fact]
    public void CriarLead_ComUserIdVazio_DeveLancarDomainException()
    {
        // Arrange & Act
        Action act = () => new Lead(Guid.Empty, "Nome Válido");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*UserId é obrigatório*");
    }

    [Fact]
    public void AtualizarStatus_DeveModificarStatusEAtualizarDataModificacao()
    {
        // Arrange
        var lead = new Lead(Guid.NewGuid(), "Academia Fitness", "Academias");
        var dataAnterior = lead.UpdatedAt.AddSeconds(-5);

        // Act
        lead.AtualizarStatus(StatusLead.Contatado);

        // Assert
        lead.Status.Should().Be(StatusLead.Contatado);
        lead.UpdatedAt.Should().BeOnOrAfter(dataAnterior);
    }

    [Fact]
    public void AtualizarObservacoes_DeveAlterarObservacao()
    {
        // Arrange
        var lead = new Lead(Guid.NewGuid(), "Restaurante Sabor", "Restaurantes");

        // Act
        lead.AtualizarObservacoes("Contato feito via WhatsApp");

        // Assert
        lead.Observacoes.Should().Be("Contato feito via WhatsApp");
    }
}
