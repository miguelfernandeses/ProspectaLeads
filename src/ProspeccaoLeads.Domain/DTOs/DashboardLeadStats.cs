using ProspeccaoLeads.Domain.Enums;

namespace ProspeccaoLeads.Domain.DTOs;

public class DashboardLeadStats
{
    public int TotalSalvos { get; set; }
    public int Contatados { get; set; }
    public int EmNegociacao { get; set; }
    public int ClientesConquistados { get; set; }
    public int NovosHoje { get; set; }

    public List<GroupCountItem> LeadsPorNicho { get; set; } = new();
    public List<GroupCountItem> LeadsPorCidade { get; set; } = new();
    public Dictionary<StatusLead, int> LeadsPorStatus { get; set; } = new();
    public List<MonthlyEvolutionItem> EvolucaoMensal { get; set; } = new();
}

public class GroupCountItem
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyEvolutionItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalCreated { get; set; }
    public int TotalConverted { get; set; }
}
