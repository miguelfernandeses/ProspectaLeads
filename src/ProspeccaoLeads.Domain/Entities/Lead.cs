using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Domain.Entities;

public class Lead
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

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
    public StatusLead Status { get; set; } = StatusLead.Novo;
    public string? Fonte { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
