using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Domain.Exceptions;

namespace ProspeccaoLeads.Domain.Entities;

public class Lead
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public string Nome { get; private set; } = string.Empty;
    public string? Categoria { get; private set; }
    public string? Telefone { get; private set; }
    public string? WhatsApp { get; private set; }
    public string? Email { get; private set; }

    public string? Endereco { get; private set; }
    public string? Cidade { get; private set; }
    public string? Estado { get; private set; }
    public string? CEP { get; private set; }

    public string? Website { get; private set; }
    public string? Instagram { get; private set; }

    public decimal? Avaliacao { get; private set; }
    public int? QuantidadeAvaliacoes { get; private set; }

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public string? Observacoes { get; private set; }
    public StatusLead Status { get; private set; } = StatusLead.Novo;
    public string? Fonte { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Construtor sem parâmetros para EF Core / Dapper
    public Lead() { }

    public Lead(
        Guid userId,
        string nome,
        string? categoria = null,
        string? telefone = null,
        string? whatsApp = null,
        string? email = null,
        string? endereco = null,
        string? cidade = null,
        string? estado = null,
        string? cep = null,
        string? website = null,
        string? instagram = null,
        decimal? avaliacao = null,
        int? quantidadeAvaliacoes = null,
        double? latitude = null,
        double? longitude = null,
        string? observacoes = null,
        StatusLead status = StatusLead.Novo,
        string? fonte = null,
        Guid? id = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("O UserId é obrigatório para cadastrar um lead.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do estabelecimento é obrigatório.");

        Id = id ?? Guid.NewGuid();
        UserId = userId;
        Nome = nome.Trim();
        Categoria = categoria?.Trim();
        Telefone = telefone?.Trim();
        WhatsApp = whatsApp?.Trim();
        Email = email?.Trim();
        Endereco = endereco?.Trim();
        Cidade = cidade?.Trim();
        Estado = estado?.Trim();
        CEP = cep?.Trim();
        Website = website?.Trim();
        Instagram = instagram?.Trim();
        Avaliacao = avaliacao;
        QuantidadeAvaliacoes = quantidadeAvaliacoes;
        Latitude = latitude;
        Longitude = longitude;
        Observacoes = observacoes?.Trim();
        Status = status;
        Fonte = fonte?.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AtualizarStatus(StatusLead novoStatus)
    {
        Status = novoStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AtualizarObservacoes(string? observacoes)
    {
        Observacoes = observacoes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AtualizarDados(
        string nome,
        string? categoria,
        string? telefone,
        string? whatsApp,
        string? email,
        string? endereco,
        string? cidade,
        string? estado,
        string? cep,
        string? website,
        string? instagram,
        decimal? avaliacao,
        int? quantidadeAvaliacoes,
        double? latitude,
        double? longitude,
        string? observacoes,
        StatusLead status,
        string? fonte)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do estabelecimento é obrigatório.");

        Nome = nome.Trim();
        Categoria = categoria?.Trim();
        Telefone = telefone?.Trim();
        WhatsApp = whatsApp?.Trim();
        Email = email?.Trim();
        Endereco = endereco?.Trim();
        Cidade = cidade?.Trim();
        Estado = estado?.Trim();
        CEP = cep?.Trim();
        Website = website?.Trim();
        Instagram = instagram?.Trim();
        Avaliacao = avaliacao;
        QuantidadeAvaliacoes = quantidadeAvaliacoes;
        Latitude = latitude;
        Longitude = longitude;
        Observacoes = observacoes?.Trim();
        Status = status;
        Fonte = fonte?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
