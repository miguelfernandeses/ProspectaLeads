using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.DTOs.Lead;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Application.Mappings;

public static class LeadMappingExtensions
{
    public static LeadDto ToDto(this Lead lead)
    {
        return new LeadDto
        {
            Id = lead.Id,
            UserId = lead.UserId,
            Nome = lead.Nome,
            Categoria = lead.Categoria,
            Telefone = lead.Telefone,
            WhatsApp = lead.WhatsApp,
            Email = lead.Email,
            Endereco = lead.Endereco,
            Cidade = lead.Cidade,
            Estado = lead.Estado,
            CEP = lead.CEP,
            Website = lead.Website,
            Instagram = lead.Instagram,
            Avaliacao = lead.Avaliacao,
            QuantidadeAvaliacoes = lead.QuantidadeAvaliacoes,
            Latitude = lead.Latitude,
            Longitude = lead.Longitude,
            Observacoes = lead.Observacoes,
            Status = lead.Status,
            Fonte = lead.Fonte,
            CreatedAt = lead.CreatedAt,
            UpdatedAt = lead.UpdatedAt
        };
    }

    public static CreateLeadDto ToCreateDto(this EstabelecimentoDto est, Guid userId)
    {
        return new CreateLeadDto
        {
            UserId = userId,
            Nome = est.Nome,
            Categoria = est.Categoria,
            Telefone = est.Telefone,
            WhatsApp = est.WhatsApp,
            Email = est.Email,
            Endereco = est.Endereco,
            Cidade = est.Cidade,
            Estado = est.Estado,
            CEP = est.CEP,
            Website = est.Website,
            Instagram = est.Instagram,
            Avaliacao = est.Avaliacao,
            QuantidadeAvaliacoes = est.QuantidadeAvaliacoes,
            Latitude = est.Latitude,
            Longitude = est.Longitude,
            Observacoes = est.Observacoes,
            Status = StatusLead.Novo,
            Fonte = est.Fonte
        };
    }
}
