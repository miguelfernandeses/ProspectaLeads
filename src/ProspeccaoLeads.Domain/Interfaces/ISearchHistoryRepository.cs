using ProspeccaoLeads.Domain.Entities;

namespace ProspeccaoLeads.Domain.Interfaces;

public interface ISearchHistoryRepository
{
    Task<SearchHistory> AddAsync(SearchHistory history, CancellationToken ct = default);
    Task<IReadOnlyList<SearchHistory>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task ClearAllByUserIdAsync(Guid userId, CancellationToken ct = default);
}
