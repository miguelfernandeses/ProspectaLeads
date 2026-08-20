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
            .AsNoTracking()
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
        var query = ApplyFilters(_context.Leads.AsNoTracking().Where(l => l.UserId == userId), search, niche, city, state, status, fromDate, toDate);

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
        var query = ApplyFilters(_context.Leads.AsNoTracking().Where(l => l.UserId == userId), search, niche, city, state, status, fromDate, toDate);
        return await query.CountAsync(ct);
    }

    public async Task<bool> ExistsByNameAndCityAsync(Guid userId, string name, string? city, CancellationToken ct = default)
    {
        var query = _context.Leads.AsNoTracking().Where(l => l.UserId == userId && l.Nome.ToLower() == name.ToLower());
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

    public async Task<ProspeccaoLeads.Domain.DTOs.DashboardLeadStats> GetDashboardStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var userLeads = _context.Leads.AsNoTracking().Where(l => l.UserId == userId);
        var hoje = DateTime.UtcNow.Date;

        var totalSalvos = await userLeads.CountAsync(ct);
        var contatados = await userLeads.CountAsync(l => l.Status == StatusLead.Contatado, ct);
        var emNegociacao = await userLeads.CountAsync(l => l.Status == StatusLead.EmNegociacao, ct);
        var clientes = await userLeads.CountAsync(l => l.Status == StatusLead.Cliente, ct);
        var novosHoje = await userLeads.CountAsync(l => l.CreatedAt >= hoje, ct);

        var nichos = await userLeads
            .GroupBy(l => string.IsNullOrEmpty(l.Categoria) ? "Não categorizado" : l.Categoria)
            .Select(g => new ProspeccaoLeads.Domain.DTOs.GroupCountItem
            {
                Key = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToListAsync(ct);

        var cidades = await userLeads
            .GroupBy(l => string.IsNullOrEmpty(l.Cidade) ? "Não informada" : l.Cidade)
            .Select(g => new ProspeccaoLeads.Domain.DTOs.GroupCountItem
            {
                Key = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToListAsync(ct);

        var statusMap = await userLeads
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

        // Evolução últimos 6 meses
        var seisMesesAtras = DateTime.UtcNow.AddMonths(-5);
        var inicioPeriodo = new DateTime(seisMesesAtras.Year, seisMesesAtras.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var leadsRecentes = await userLeads
            .Where(l => l.CreatedAt >= inicioPeriodo || l.UpdatedAt >= inicioPeriodo)
            .Select(l => new { l.CreatedAt, l.UpdatedAt, l.Status })
            .ToListAsync(ct);

        var evolucao = new List<ProspeccaoLeads.Domain.DTOs.MonthlyEvolutionItem>();
        for (int i = 5; i >= 0; i--)
        {
            var mesRef = DateTime.UtcNow.AddMonths(-i);
            var totalCriadosMes = leadsRecentes.Count(l => l.CreatedAt.Year == mesRef.Year && l.CreatedAt.Month == mesRef.Month);
            var totalConvertidosMes = leadsRecentes.Count(l => l.Status == StatusLead.Cliente && l.UpdatedAt.Year == mesRef.Year && l.UpdatedAt.Month == mesRef.Month);

            evolucao.Add(new ProspeccaoLeads.Domain.DTOs.MonthlyEvolutionItem
            {
                Year = mesRef.Year,
                Month = mesRef.Month,
                TotalCreated = totalCriadosMes,
                TotalConverted = totalConvertidosMes
            });
        }

        return new ProspeccaoLeads.Domain.DTOs.DashboardLeadStats
        {
            TotalSalvos = totalSalvos,
            Contatados = contatados,
            EmNegociacao = emNegociacao,
            ClientesConquistados = clientes,
            NovosHoje = novosHoje,
            LeadsPorNicho = nichos,
            LeadsPorCidade = cidades,
            LeadsPorStatus = statusMap,
            EvolucaoMensal = evolucao
        };
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
