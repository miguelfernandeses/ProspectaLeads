using System.ComponentModel.DataAnnotations;
using ProspeccaoLeads.Application.Common.Helpers;

namespace ProspeccaoLeads.Application.DTOs.Estabelecimento;

public class BuscaEstabelecimentoDto
{
    [Required(ErrorMessage = "Informe o nicho ou segmento de negócio.")]
    [MinLength(2, ErrorMessage = "O nicho deve conter ao menos 2 caracteres.")]
    public string Nicho { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a localização (cidade/estado).")]
    [MinLength(2, ErrorMessage = "A localização deve conter ao menos 2 caracteres.")]
    public string Localizacao { get; set; } = string.Empty;

    public int MaxResultados { get; set; } = 30;
}

public class EstabelecimentoDto
{
    public Guid? Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }

    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? CEP { get; set; }

    public string? Website { get; set; }
    public string? Instagram { get; set; }

    public decimal? Avaliacao { get; set; }
    public int? QuantidadeAvaliacoes { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Observacoes { get; set; }
    public string? Fonte { get; set; }

    public bool JaSalvoComoLead { get; set; }
    public Guid? LeadId { get; set; }

    public string GetWhatsAppUrl() => ExternalLinkHelper.GerarWhatsAppUrl(WhatsApp ?? Telefone);

    public string GetGoogleMapsUrl() => ExternalLinkHelper.GerarGoogleMapsUrl(Nome, Endereco, Cidade, Estado);

    public string GetInstagramUrl() => ExternalLinkHelper.GerarInstagramUrl(Instagram);
}

