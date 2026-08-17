using ProspeccaoLeads.Application.Common;
using ProspeccaoLeads.Application.DTOs.Estabelecimento;
using ProspeccaoLeads.Application.DTOs.Lead;
using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Application.Interfaces;

public interface ILeadService
{
    Task<IReadOnlyList<LeadDto>> ObterTodosAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResultDto<LeadDto>> ObterPaginadoAsync(Guid userId, LeadFilterDto filter, CancellationToken ct = default);
    Task<LeadDto?> ObterPorIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<LeadDto>> CriarAsync(CreateLeadDto dto, CancellationToken ct = default);
    Task<Result<LeadDto>> SalvarEstabelecimentoAsync(EstabelecimentoDto estabelecimento, Guid userId, CancellationToken ct = default);
    Task<Result> AtualizarAsync(UpdateLeadDto dto, Guid userId, CancellationToken ct = default);
    Task<Result> AtualizarStatusAsync(Guid id, StatusLead novoStatus, Guid userId, CancellationToken ct = default);
    Task<Result> AtualizarObservacoesAsync(Guid id, string observacoes, Guid userId, CancellationToken ct = default);
    Task<Result> ExcluirAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<bool> VerificarDuplicadoAsync(string nome, string? cidade, Guid userId, CancellationToken ct = default);
}
