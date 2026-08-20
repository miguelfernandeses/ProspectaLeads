using ProspeccaoLeads.Application.Common;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.DTOs.Lead;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Application.Mappings;
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
        return leads.Select(l => l.ToDto()).ToList();
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
            Items = items.Select(l => l.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LeadDto?> ObterPorIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, userId, ct);
        return lead?.ToDto();
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
        return Result<LeadDto>.Success(created.ToDto());
    }

    public async Task<Result<LeadDto>> SalvarEstabelecimentoAsync(EstabelecimentoDto estabelecimento, Guid userId, CancellationToken ct = default)
    {
        var dto = estabelecimento.ToCreateDto(userId);
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
}

