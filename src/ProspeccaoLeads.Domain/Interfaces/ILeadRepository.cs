using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Domain.Interfaces;

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Lead>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Lead>> GetFilteredAsync(
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
        CancellationToken ct = default);
    Task<int> CountFilteredAsync(
        Guid userId,
        string? search = null,
        string? niche = null,
        string? city = null,
        string? state = null,
        StatusLead? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);
    Task<bool> ExistsByNameAndCityAsync(Guid userId, string name, string? city, CancellationToken ct = default);
    Task<Lead> AddAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
    Task DeleteAsync(Lead lead, CancellationToken ct = default);
    Task<ProspeccaoLeads.Domain.DTOs.DashboardLeadStats> GetDashboardStatsAsync(Guid userId, CancellationToken ct = default);
}
