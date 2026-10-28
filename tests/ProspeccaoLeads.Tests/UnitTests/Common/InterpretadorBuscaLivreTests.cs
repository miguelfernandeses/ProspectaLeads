using FluentAssertions;
using ProspeccaoLeads.Application.Common.Search;

namespace ProspeccaoLeads.Tests.UnitTests.Common;

public class InterpretadorBuscaLivreTests
{
    [Theory]
    [InlineData("mecânica Limeira", "mecânica", "Limeira - SP")]
    [InlineData("oficina em Rio Claro", "oficina", "Rio Claro - SP")]
    [InlineData("AUTO CENTER sao paulo", "AUTO CENTER", "São Paulo - SP")]
    public void Interpretar_ComCidadeConhecidaNoFinal_DeveSepararRamoELocalizacao(
        string consulta,
        string nichoEsperado,
        string localEsperado)
    {
        var resultado = InterpretadorBuscaLivre.Interpretar(consulta, null);

        resultado.Nicho.Should().Be(nichoEsperado);
        resultado.Localizacao.Should().Be(localEsperado);
        resultado.LocalizacaoInferida.Should().BeTrue();
    }

    [Fact]
    public void Interpretar_ComLocalizacaoExplicita_DevePreservarConsultaLivre()
    {
        var resultado = InterpretadorBuscaLivre.Interpretar(
            "manutenção de máquinas agrícolas",
            "Limeira - SP");

        resultado.Nicho.Should().Be("manutenção de máquinas agrícolas");
        resultado.Localizacao.Should().Be("Limeira - SP");
        resultado.LocalizacaoInferida.Should().BeFalse();
    }

    [Fact]
    public void Interpretar_ComCidadeDesconhecida_NaoDeveInventarLocalizacao()
    {
        var resultado = InterpretadorBuscaLivre.Interpretar("floricultura Cidade Nova", null);

        resultado.Nicho.Should().Be("floricultura Cidade Nova");
        resultado.Localizacao.Should().BeEmpty();
        resultado.LocalizacaoInferida.Should().BeFalse();
    }
}
