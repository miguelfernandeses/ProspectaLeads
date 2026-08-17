using ProspeccaoLeads.Application.DTOs.SearchHistory;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Interfaces;

namespace ProspeccaoLeads.Application.Services;

public class SearchHistoryService : ISearchHistoryService
{
    private readonly ISearchHistoryRepository _historyRepository;

    public SearchHistoryService(ISearchHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task<IReadOnlyList<SearchHistoryDto>> ObterHistoricoAsync(Guid userId, int limit = 50, CancellationToken ct = default)
    {
        var list = await _historyRepository.GetByUserIdAsync(userId, limit, ct);
        return list.Select(h => new SearchHistoryDto
        {
            Id = h.Id,
            UserId = h.UserId,
            Niche = h.Niche,
            Location = h.Location,
            ResultCount = h.ResultCount,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    public async Task RegistrarBuscaAsync(Guid userId, string nicho, string localizacao, int quantidadeResultados, CancellationToken ct = default)
    {
        var history = new SearchHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Niche = nicho.Trim(),
            Location = localizacao.Trim(),
            ResultCount = quantidadeResultados,
            CreatedAt = DateTime.UtcNow
        };

        await _historyRepository.AddAsync(history, ct);
    }

    public async Task ExcluirAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await _historyRepository.DeleteAsync(id, userId, ct);
    }

    public async Task LimparHistoricoAsync(Guid userId, CancellationToken ct = default)
    {
        await _historyRepository.ClearAllByUserIdAsync(userId, ct);
    }
}
