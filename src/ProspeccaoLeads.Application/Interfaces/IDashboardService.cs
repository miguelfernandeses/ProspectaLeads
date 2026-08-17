using ProspeccaoLeads.Application.DTOs.Dashboard;

namespace ProspeccaoLeads.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> ObterResumoAsync(Guid userId, CancellationToken ct = default);
}
