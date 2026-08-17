using System.Text;
using FluentAssertions;
using ProspeccaoLeads.Application.DTOs.Lead;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Infrastructure.Export;
using Xunit;

namespace ProspeccaoLeads.Tests.UnitTests.Services;

public class ExportServiceTests
{
    private readonly ExportService _service;

    public ExportServiceTests()
    {
        _service = new ExportService();
    }

    [Fact]
    public async Task ExportarParaCsvAsync_DeveConterCabecalhoEConteudoComUtf8Bom()
    {
        // Arrange
        var leads = new List<LeadDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Clínica São Paulo Odonto",
                Categoria = "Clínica odontológica",
                Cidade = "São Paulo",
                Estado = "SP",
                Telefone = "(11) 99999-8888",
                Status = StatusLead.Interessado,
                Avaliacao = 4.8m,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var bytes = await _service.ExportarParaCsvAsync(leads);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);

        // Verificar UTF-8 BOM
        var preamble = Encoding.UTF8.GetPreamble();
        bytes.Take(preamble.Length).Should().Equal(preamble);

        var csvText = Encoding.UTF8.GetString(bytes);
        csvText.Should().Contain("Nome;Categoria;Telefone");
        csvText.Should().Contain("Clínica São Paulo Odonto");
        csvText.Should().Contain("Interessado");
    }

    [Fact]
    public async Task ExportarParaExcelAsync_DeveGerarArquivoXlsxValido()
    {
        // Arrange
        var leads = new List<LeadDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Restaurante Sabor Brasil",
                Categoria = "Restaurante",
                Cidade = "Campinas",
                Estado = "SP",
                Status = StatusLead.Cliente,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var bytes = await _service.ExportarParaExcelAsync(leads);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);

        // O cabeçalho de um arquivo zip/xlsx começa com 'PK' (0x50, 0x4B)
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }
}
