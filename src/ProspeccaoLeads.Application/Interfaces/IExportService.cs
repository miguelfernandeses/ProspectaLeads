using ProspeccaoLeads.Application.DTOs.Lead;

namespace ProspeccaoLeads.Application.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportarParaCsvAsync(IEnumerable<LeadDto> leads, CancellationToken ct = default);
    Task<byte[]> ExportarParaExcelAsync(IEnumerable<LeadDto> leads, CancellationToken ct = default);
}
