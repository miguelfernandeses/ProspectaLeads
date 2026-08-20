using System.ComponentModel.DataAnnotations;
using ProspeccaoLeads.Application.Common.Helpers;
using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Application.DTOs.Lead;

public class LeadDto
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
    public StatusLead Status { get; set; }
    public string? Fonte { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string StatusFormatado => Status.ObterLabel();

    public string GetWhatsAppUrl() => ExternalLinkHelper.GerarWhatsAppUrl(WhatsApp ?? Telefone);

    public string GetGoogleMapsUrl() => ExternalLinkHelper.GerarGoogleMapsUrl(Nome, Endereco, Cidade, Estado);

    public string GetInstagramUrl() => ExternalLinkHelper.GerarInstagramUrl(Instagram);
}

public class CreateLeadDto
{
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "O nome do estabelecimento é obrigatório.")]
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
}

public class UpdateLeadDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome do estabelecimento é obrigatório.")]
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
    public StatusLead Status { get; set; }
    public string? Fonte { get; set; }
}

public class UpdateLeadStatusDto
{
    public Guid Id { get; set; }
    public StatusLead Status { get; set; }
}

public class LeadFilterDto
{
    public string? Search { get; set; }
    public string? Niche { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public StatusLead? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
