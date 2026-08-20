using FluentAssertions;
using ProspeccaoLeads.Application.Common.Helpers;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.Mappings;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;
using Xunit;

namespace ProspeccaoLeads.Tests.UnitTests.Common;

public class HelpersAndMappingsTests
{
    [Fact]
    public void GerarWhatsAppUrl_ComNumeroValido_DeveFormatarComDDI55()
    {
        // Arrange
        var numero = "(19) 98765-4321";

        // Act
        var url = ExternalLinkHelper.GerarWhatsAppUrl(numero);

        // Assert
        url.Should().Be("https://wa.me/5519987654321");
    }

    [Fact]
    public void GerarWhatsAppUrl_Vazio_DeveRetornarVazio()
    {
        ExternalLinkHelper.GerarWhatsAppUrl(null).Should().BeEmpty();
        ExternalLinkHelper.GerarWhatsAppUrl("   ").Should().BeEmpty();
    }

    [Fact]
    public void GerarGoogleMapsUrl_ComEndereco_DeveGerarUrlPesquisa()
    {
        // Arrange & Act
        var url = ExternalLinkHelper.GerarGoogleMapsUrl("Clinica X", "Av Paulista, 1000", "São Paulo", "SP");

        // Assert
        url.Should().Contain("https://www.google.com/maps/search/?api=1&query=");
        url.Should().Contain("Clinica%20X");
    }

    [Fact]
    public void GerarInstagramUrl_ComArroba_DeveRemoverArroba()
    {
        // Arrange & Act
        var url = ExternalLinkHelper.GerarInstagramUrl("@minha_empresa");

        // Assert
        url.Should().Be("https://instagram.com/minha_empresa");
    }

    [Fact]
    public void LeadMappingExtensions_ToDto_DeveMapearTodasAsPropriedades()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lead = new Lead(
            userId: userId,
            nome: "Imobiliária Aliança",
            categoria: "Imobiliárias",
            telefone: "(19) 3541-1122",
            whatsApp: "(19) 99887-7665",
            email: "contato@alianca.com.br",
            endereco: "Rua Tiradentes, 500",
            cidade: "Araras",
            estado: "SP",
            cep: "13600-000",
            website: "https://alianca.com.br",
            instagram: "@alianca_imoveis",
            avaliacao: 4.8m,
            quantidadeAvaliacoes: 45,
            observacoes: "Lead quente",
            status: StatusLead.EmNegociacao,
            fonte: "RegionalRealPlaces"
        );

        // Act
        var dto = lead.ToDto();

        // Assert
        dto.Id.Should().Be(lead.Id);
        dto.UserId.Should().Be(userId);
        dto.Nome.Should().Be("Imobiliária Aliança");
        dto.Categoria.Should().Be("Imobiliárias");
        dto.Status.Should().Be(StatusLead.EmNegociacao);
        dto.StatusFormatado.Should().Be("Em Negociação");
        dto.GetWhatsAppUrl().Should().Be("https://wa.me/5519998877665");
    }

    [Fact]
    public void EstabelecimentoDto_ToCreateDto_DeveMapearParaCreateLeadDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var est = new EstabelecimentoDto
        {
            Nome = "Oficina do Zé",
            Categoria = "Oficina Mecânica",
            Telefone = "(19) 3542-9999",
            Cidade = "Leme",
            Estado = "SP"
        };

        // Act
        var createDto = est.ToCreateDto(userId);

        // Assert
        createDto.UserId.Should().Be(userId);
        createDto.Nome.Should().Be("Oficina do Zé");
        createDto.Categoria.Should().Be("Oficina Mecânica");
        createDto.Cidade.Should().Be("Leme");
        createDto.Status.Should().Be(StatusLead.Novo);
    }
}
