using Microsoft.EntityFrameworkCore;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Domain.Interfaces;
using ProspeccaoLeads.Infrastructure.Data;

namespace ProspeccaoLeads.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly AppDbContext _context;

    public LeadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Lead?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await _context.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<Lead>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Leads
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Lead>> GetFilteredAsync(
        Guid userId,
        string? search = null,
        string? niche = null,
        string? city = null,
        string? state = null,
        StatusLead? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(_context.Leads.Where(l => l.UserId == userId), search, niche, city, state, status, fromDate, toDate);

        // Sorting
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "nome" => sortDescending ? query.OrderByDescending(l => l.Nome) : query.OrderBy(l => l.Nome),
            "avaliacao" => sortDescending ? query.OrderByDescending(l => l.Avaliacao) : query.OrderBy(l => l.Avaliacao),
            "status" => sortDescending ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
            "cidade" => sortDescending ? query.OrderByDescending(l => l.Cidade) : query.OrderBy(l => l.Cidade),
            "categoria" => sortDescending ? query.OrderByDescending(l => l.Categoria) : query.OrderBy(l => l.Categoria),
            _ => sortDescending ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountFilteredAsync(
        Guid userId,
        string? search = null,
        string? niche = null,
        string? city = null,
        string? state = null,
        StatusLead? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(_context.Leads.Where(l => l.UserId == userId), search, niche, city, state, status, fromDate, toDate);
        return await query.CountAsync(ct);
    }

    public async Task<bool> ExistsByNameAndCityAsync(Guid userId, string name, string? city, CancellationToken ct = default)
    {
        var query = _context.Leads.Where(l => l.UserId == userId && l.Nome.ToLower() == name.ToLower());
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(l => l.Cidade != null && l.Cidade.ToLower() == city.ToLower());
        }
        return await query.AnyAsync(ct);
    }

    public async Task<Lead> AddAsync(Lead lead, CancellationToken ct = default)
    {
        await _context.Leads.AddAsync(lead, ct);
        await _context.SaveChangesAsync(ct);
        return lead;
    }

    public async Task UpdateAsync(Lead lead, CancellationToken ct = default)
    {
        _context.Leads.Update(lead);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Lead lead, CancellationToken ct = default)
    {
        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync(ct);
    }

    private static IQueryable<Lead> ApplyFilters(
        IQueryable<Lead> query,
        string? search,
        string? niche,
        string? city,
        string? state,
        StatusLead? status,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.Nome, $"%{s}%") ||
                (l.Telefone != null && EF.Functions.ILike(l.Telefone, $"%{s}%")) ||
                (l.Email != null && EF.Functions.ILike(l.Email, $"%{s}%")) ||
                (l.Endereco != null && EF.Functions.ILike(l.Endereco, $"%{s}%")) ||
                (l.Observacoes != null && EF.Functions.ILike(l.Observacoes, $"%{s}%")) ||
                (l.Cidade != null && EF.Functions.ILike(l.Cidade, $"%{s}%")) ||
                (l.Categoria != null && EF.Functions.ILike(l.Categoria, $"%{s}%")));
        }

        if (!string.IsNullOrWhiteSpace(niche))
        {
            var n = niche.Trim();
            query = query.Where(l => l.Categoria != null && EF.Functions.ILike(l.Categoria, $"%{n}%"));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim();
            query = query.Where(l => l.Cidade != null && EF.Functions.ILike(l.Cidade, $"%{c}%"));
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            var st = state.Trim();
            query = query.Where(l => l.Estado != null && EF.Functions.ILike(l.Estado, $"%{st}%"));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= toDate.Value);
        }

        return query;
    }
}
