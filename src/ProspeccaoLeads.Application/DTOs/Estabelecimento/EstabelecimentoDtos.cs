using System.ComponentModel.DataAnnotations;

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

    public string GetWhatsAppUrl()
    {
        var num = WhatsApp ?? Telefone;
        if (string.IsNullOrWhiteSpace(num)) return string.Empty;
        var digits = new string(num.Where(char.IsDigit).ToArray());
        if (digits.Length == 10 || digits.Length == 11)
        {
            digits = "55" + digits;
        }
        return $"https://wa.me/{digits}";
    }

    public string GetGoogleMapsUrl()
    {
        var localParts = new[] { Endereco, Cidade, Estado }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var enderecoCompleto = string.Join(", ", localParts);

        var termo = string.IsNullOrWhiteSpace(enderecoCompleto)
            ? $"{Nome} {Cidade} {Estado}".Trim()
            : $"{Nome}, {enderecoCompleto}".Trim();

        var query = Uri.EscapeDataString(termo);
        return $"https://www.google.com/maps/search/?api=1&query={query}";
    }

    public string GetInstagramUrl()
    {
        if (string.IsNullOrWhiteSpace(Instagram)) return string.Empty;
        var handle = Instagram.Trim().TrimStart('@');
        if (handle.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return handle;
        return $"https://instagram.com/{handle}";
    }
}
