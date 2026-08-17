using ProspeccaoLeads.Application.DTOs.SearchHistory;

namespace ProspeccaoLeads.Application.Interfaces;

public interface ISearchHistoryService
{
    Task<IReadOnlyList<SearchHistoryDto>> ObterHistoricoAsync(Guid userId, int limit = 50, CancellationToken ct = default);
    Task RegistrarBuscaAsync(Guid userId, string nicho, string localizacao, int quantidadeResultados, CancellationToken ct = default);
    Task ExcluirAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task LimparHistoricoAsync(Guid userId, CancellationToken ct = default);
}
