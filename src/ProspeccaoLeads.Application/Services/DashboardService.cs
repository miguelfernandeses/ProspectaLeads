using ProspeccaoLeads.Application.DTOs.Dashboard;
using ProspeccaoLeads.Application.Interfaces;
using ProspeccaoLeads.Domain.Enums;
using ProspeccaoLeads.Domain.Interfaces;

namespace ProspeccaoLeads.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ILeadRepository _leadRepository;
    private readonly ISearchHistoryRepository _historyRepository;

    public DashboardService(ILeadRepository leadRepository, ISearchHistoryRepository historyRepository)
    {
        _leadRepository = leadRepository;
        _historyRepository = historyRepository;
    }

    public async Task<DashboardSummaryDto> ObterResumoAsync(Guid userId, CancellationToken ct = default)
    {
        var stats = await _leadRepository.GetDashboardStatsAsync(userId, ct);
        var historico = await _historyRepository.GetByUserIdAsync(userId, 500, ct);

        var totalEncontrados = historico.Sum(h => h.ResultCount);
        var totalSalvos = stats.TotalSalvos;
        var clientes = stats.ClientesConquistados;

        decimal taxaConversao = 0;
        if (totalSalvos > 0)
        {
            taxaConversao = Math.Round(((decimal)clientes / totalSalvos) * 100, 1);
        }

        // Gráfico: Leads por Nicho
        var leadsPorNicho = stats.LeadsPorNicho
            .Select((item, i) => new ChartItemDto
            {
                Label = item.Key,
                Value = item.Count,
                Color = ObterCorParaIndice(i)
            })
            .ToList();

        // Gráfico: Leads por Cidade
        var leadsPorCidade = stats.LeadsPorCidade
            .Select((item, i) => new ChartItemDto
            {
                Label = item.Key,
                Value = item.Count,
                Color = ObterCorParaIndice(i)
            })
            .ToList();

        // Gráfico: Leads por Status (utilizando StatusLeadExtensions centralizado)
        var leadsPorStatus = Enum.GetValues<StatusLead>()
            .Select(s => new ChartItemDto
            {
                Label = s.ObterLabel(),
                Value = stats.LeadsPorStatus.GetValueOrDefault(s, 0),
                Color = s.ObterCorHex()
            })
            .Where(c => c.Value > 0 || totalSalvos == 0)
            .ToList();

        // Gráfico: Evolução Mensal (Últimos 6 meses)
        var evolucaoMensal = stats.EvolucaoMensal
            .Select(m =>
            {
                var dt = new DateTime(m.Year, m.Month, 1);
                return new ChartItemDto
                {
                    Label = dt.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                    Value = m.TotalCreated,
                    SecondaryValue = m.TotalConverted,
                    Color = "#6366F1"
                };
            })
            .ToList();

        return new DashboardSummaryDto
        {
            TotalEncontrados = totalEncontrados,
            TotalSalvos = totalSalvos,
            Contatados = stats.Contatados,
            EmNegociacao = stats.EmNegociacao,
            ClientesConquistados = clientes,
            TaxaConversao = taxaConversao,
            NovosHoje = stats.NovosHoje,
            LeadsPorNicho = leadsPorNicho,
            LeadsPorCidade = leadsPorCidade,
            LeadsPorStatus = leadsPorStatus,
            EvolucaoMensal = evolucaoMensal
        };
    }

    private static readonly string[] ChartColors =
    [
        "#6366F1", // Indigo
        "#10B981", // Emerald
        "#F59E0B", // Amber
        "#EC4899", // Pink
        "#8B5CF6", // Violet
        "#06B6D4", // Cyan
        "#F97316", // Orange
        "#14B8A6"  // Teal
    ];

    private static string ObterCorParaIndice(int index) => ChartColors[index % ChartColors.Length];
}
