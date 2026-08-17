using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalEncontrados { get; set; }
    public int TotalSalvos { get; set; }
    public int Contatados { get; set; }
    public int EmNegociacao { get; set; }
    public int ClientesConquistados { get; set; }
    public decimal TaxaConversao { get; set; }
    public int NovosHoje { get; set; }

    public List<ChartItemDto> LeadsPorNicho { get; set; } = new();
    public List<ChartItemDto> LeadsPorCidade { get; set; } = new();
    public List<ChartItemDto> LeadsPorStatus { get; set; } = new();
    public List<ChartItemDto> EvolucaoMensal { get; set; } = new();
}

public class ChartItemDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public double? SecondaryValue { get; set; }
    public string? Color { get; set; }
}
