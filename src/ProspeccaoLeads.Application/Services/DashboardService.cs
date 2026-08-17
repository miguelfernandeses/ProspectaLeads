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
        var leads = await _leadRepository.GetAllAsync(userId, ct);
        var historico = await _historyRepository.GetByUserIdAsync(userId, 500, ct);

        var totalEncontrados = historico.Sum(h => h.ResultCount);
        var totalSalvos = leads.Count;
        var contatados = leads.Count(l => l.Status == StatusLead.Contatado);
        var emNegociacao = leads.Count(l => l.Status == StatusLead.EmNegociacao);
        var clientes = leads.Count(l => l.Status == StatusLead.Cliente);
        var hoje = DateTime.UtcNow.Date;
        var novosHoje = leads.Count(l => l.CreatedAt.Date == hoje);

        decimal taxaConversao = 0;
        if (totalSalvos > 0)
        {
            taxaConversao = Math.Round(((decimal)clientes / totalSalvos) * 100, 1);
        }

        // Gráfico: Leads por Nicho
        var leadsPorNicho = leads
            .GroupBy(l => string.IsNullOrWhiteSpace(l.Categoria) ? "Não categorizado" : l.Categoria.Trim())
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select((g, i) => new ChartItemDto
            {
                Label = g.Key,
                Value = g.Count(),
                Color = ObterCorParaIndice(i)
            })
            .ToList();

        // Gráfico: Leads por Cidade
        var leadsPorCidade = leads
            .GroupBy(l => string.IsNullOrWhiteSpace(l.Cidade) ? "Não informada" : l.Cidade.Trim())
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select((g, i) => new ChartItemDto
            {
                Label = g.Key,
                Value = g.Count(),
                Color = ObterCorParaIndice(i)
            })
            .ToList();

        // Gráfico: Leads por Status
        var statusCores = new Dictionary<StatusLead, string>
        {
            { StatusLead.Novo, "#3B82F6" },          // Blue
            { StatusLead.Interessado, "#8B5CF6" },   // Purple
            { StatusLead.Contatado, "#F59E0B" },     // Amber
            { StatusLead.EmNegociacao, "#EC4899" },  // Pink
            { StatusLead.Cliente, "#10B981" },       // Emerald
            { StatusLead.SemInteresse, "#6B7280" }   // Gray
        };

        var statusLabels = new Dictionary<StatusLead, string>
        {
            { StatusLead.Novo, "Novo" },
            { StatusLead.Interessado, "Interessado" },
            { StatusLead.Contatado, "Contatado" },
            { StatusLead.EmNegociacao, "Em Negociação" },
            { StatusLead.Cliente, "Cliente" },
            { StatusLead.SemInteresse, "Sem Interesse" }
        };

        var leadsPorStatus = Enum.GetValues<StatusLead>()
            .Select(s => new ChartItemDto
            {
                Label = statusLabels[s],
                Value = leads.Count(l => l.Status == s),
                Color = statusCores.GetValueOrDefault(s, "#6366F1")
            })
            .Where(c => c.Value > 0 || leads.Count == 0)
            .ToList();

        // Gráfico: Evolução Mensal (Últimos 6 meses)
        var evolucaoMensal = new List<ChartItemDto>();
        for (int i = 5; i >= 0; i--)
        {
            var mesRef = DateTime.UtcNow.AddMonths(-i);
            var totalMes = leads.Count(l => l.CreatedAt.Year == mesRef.Year && l.CreatedAt.Month == mesRef.Month);
            var clientesMes = leads.Count(l => l.Status == StatusLead.Cliente && l.UpdatedAt.Year == mesRef.Year && l.UpdatedAt.Month == mesRef.Month);

            evolucaoMensal.Add(new ChartItemDto
            {
                Label = mesRef.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                Value = totalMes,
                SecondaryValue = clientesMes,
                Color = "#6366F1"
            });
        }

        return new DashboardSummaryDto
        {
            TotalEncontrados = totalEncontrados,
            TotalSalvos = totalSalvos,
            Contatados = contatados,
            EmNegociacao = emNegociacao,
            ClientesConquistados = clientes,
            TaxaConversao = taxaConversao,
            NovosHoje = novosHoje,
            LeadsPorNicho = leadsPorNicho,
            LeadsPorCidade = leadsPorCidade,
            LeadsPorStatus = leadsPorStatus,
            EvolucaoMensal = evolucaoMensal
        };
    }

    private static string ObterCorParaIndice(int index)
    {
        var cores = new[]
        {
            "#6366F1", // Indigo
            "#10B981", // Emerald
            "#F59E0B", // Amber
            "#EC4899", // Pink
            "#8B5CF6", // Violet
            "#06B6D4", // Cyan
            "#F97316", // Orange
            "#14B8A6"  // Teal
        };
        return cores[index % cores.Length];
    }
}
