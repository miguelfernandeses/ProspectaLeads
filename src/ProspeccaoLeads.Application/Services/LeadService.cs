using ProspeccaoLeads.Application.Common;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.DTOs.Lead;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Domain.Interfaces;

namespace ProspeccaoLeads.Application.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepository _leadRepository;

    public LeadService(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<IReadOnlyList<LeadDto>> ObterTodosAsync(Guid userId, CancellationToken ct = default)
    {
        var leads = await _leadRepository.GetAllAsync(userId, ct);
        return leads.Select(MapToDto).ToList();
    }

    public async Task<PagedResultDto<LeadDto>> ObterPaginadoAsync(Guid userId, LeadFilterDto filter, CancellationToken ct = default)
    {
        var totalCount = await _leadRepository.CountFilteredAsync(
            userId,
            filter.Search,
            filter.Niche,
            filter.City,
            filter.State,
            filter.Status,
            filter.FromDate,
            filter.ToDate,
            ct);

        var items = await _leadRepository.GetFilteredAsync(
            userId,
            filter.Search,
            filter.Niche,
            filter.City,
            filter.State,
            filter.Status,
            filter.FromDate,
            filter.ToDate,
            filter.SortBy,
            filter.SortDescending,
            filter.Page,
            filter.PageSize,
            ct);

        return new PagedResultDto<LeadDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LeadDto?> ObterPorIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, userId, ct);
        return lead == null ? null : MapToDto(lead);
    }

    public async Task<Result<LeadDto>> CriarAsync(CreateLeadDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            return Result<LeadDto>.Failure("O nome do estabelecimento é obrigatório.");
        }

        var existe = await _leadRepository.ExistsByNameAndCityAsync(dto.UserId, dto.Nome.Trim(), dto.Cidade?.Trim(), ct);
        if (existe)
        {
            return Result<LeadDto>.Failure($"O estabelecimento '{dto.Nome}' já está cadastrado em seus leads.");
        }

        var lead = new Lead(
            userId: dto.UserId,
            nome: dto.Nome,
            categoria: dto.Categoria,
            telefone: dto.Telefone,
            whatsApp: dto.WhatsApp,
            email: dto.Email,
            endereco: dto.Endereco,
            cidade: dto.Cidade,
            estado: dto.Estado,
            cep: dto.CEP,
            website: dto.Website,
            instagram: dto.Instagram,
            avaliacao: dto.Avaliacao,
            quantidadeAvaliacoes: dto.QuantidadeAvaliacoes,
            latitude: dto.Latitude,
            longitude: dto.Longitude,
            observacoes: dto.Observacoes,
            status: dto.Status,
            fonte: dto.Fonte
        );

        var created = await _leadRepository.AddAsync(lead, ct);
        return Result<LeadDto>.Success(MapToDto(created));
    }

    public async Task<Result<LeadDto>> SalvarEstabelecimentoAsync(EstabelecimentoDto estabelecimento, Guid userId, CancellationToken ct = default)
    {
        var dto = new CreateLeadDto
        {
            UserId = userId,
            Nome = estabelecimento.Nome,
            Categoria = estabelecimento.Categoria,
            Telefone = estabelecimento.Telefone,
            WhatsApp = estabelecimento.WhatsApp,
            Email = estabelecimento.Email,
            Endereco = estabelecimento.Endereco,
            Cidade = estabelecimento.Cidade,
            Estado = estabelecimento.Estado,
            CEP = estabelecimento.CEP,
            Website = estabelecimento.Website,
            Instagram = estabelecimento.Instagram,
            Avaliacao = estabelecimento.Avaliacao,
            QuantidadeAvaliacoes = estabelecimento.QuantidadeAvaliacoes,
            Latitude = estabelecimento.Latitude,
            Longitude = estabelecimento.Longitude,
            Observacoes = estabelecimento.Observacoes,
            Status = StatusLead.Novo,
            Fonte = estabelecimento.Fonte
        };

        return await CriarAsync(dto, ct);
    }

    public async Task<Result> AtualizarAsync(UpdateLeadDto dto, Guid userId, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(dto.Id, userId, ct);
        if (lead == null)
        {
            return Result.Failure("Lead não encontrado.");
        }

        lead.AtualizarDados(
            nome: dto.Nome,
            categoria: dto.Categoria,
            telefone: dto.Telefone,
            whatsApp: dto.WhatsApp,
            email: dto.Email,
            endereco: dto.Endereco,
            cidade: dto.Cidade,
            estado: dto.Estado,
            cep: dto.CEP,
            website: dto.Website,
            instagram: dto.Instagram,
            avaliacao: dto.Avaliacao,
            quantidadeAvaliacoes: dto.QuantidadeAvaliacoes,
            latitude: dto.Latitude,
            longitude: dto.Longitude,
            observacoes: dto.Observacoes,
            status: dto.Status,
            fonte: dto.Fonte
        );

        await _leadRepository.UpdateAsync(lead, ct);
        return Result.Success();
    }

    public async Task<Result> AtualizarStatusAsync(Guid id, StatusLead novoStatus, Guid userId, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, userId, ct);
        if (lead == null)
        {
            return Result.Failure("Lead não encontrado.");
        }

        lead.AtualizarStatus(novoStatus);

        await _leadRepository.UpdateAsync(lead, ct);
        return Result.Success();
    }

    public async Task<Result> AtualizarObservacoesAsync(Guid id, string observacoes, Guid userId, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, userId, ct);
        if (lead == null)
        {
            return Result.Failure("Lead não encontrado.");
        }

        lead.AtualizarObservacoes(observacoes);

        await _leadRepository.UpdateAsync(lead, ct);
        return Result.Success();
    }

    public async Task<Result> ExcluirAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, userId, ct);
        if (lead == null)
        {
            return Result.Failure("Lead não encontrado.");
        }

        await _leadRepository.DeleteAsync(lead, ct);
        return Result.Success();
    }

    public async Task<bool> VerificarDuplicadoAsync(string nome, string? cidade, Guid userId, CancellationToken ct = default)
    {
        return await _leadRepository.ExistsByNameAndCityAsync(userId, nome.Trim(), cidade?.Trim(), ct);
    }

    private static LeadDto MapToDto(Lead lead)
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
}
