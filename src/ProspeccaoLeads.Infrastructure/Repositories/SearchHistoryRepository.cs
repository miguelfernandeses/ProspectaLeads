using Microsoft.EntityFrameworkCore;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Interfaces;
using ProspeccaoLeads.Infrastructure.Data;

namespace ProspeccaoLeads.Infrastructure.Repositories;

public class SearchHistoryRepository : ISearchHistoryRepository
{
    private readonly AppDbContext _context;

    public SearchHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SearchHistory> AddAsync(SearchHistory history, CancellationToken ct = default)
    {
        await _context.Searches.AddAsync(history, ct);
        await _context.SaveChangesAsync(ct);
        return history;
    }

    public async Task<IReadOnlyList<SearchHistory>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken ct = default)
    {
        return await _context.Searches
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var history = await _context.Searches
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

        if (history != null)
        {
            _context.Searches.Remove(history);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task ClearAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _context.Searches
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        if (list.Count > 0)
        {
            _context.Searches.RemoveRange(list);
            await _context.SaveChangesAsync(ct);
        }
    }
}
