namespace ProspeccaoLeads.Domain.Enums;

public static class StatusLeadExtensions
{
    public static string ObterLabel(this StatusLead status)
    {
        return status switch
        {
            StatusLead.Novo => "Novo",
            StatusLead.Interessado => "Interessado",
            StatusLead.Contatado => "Contatado",
            StatusLead.EmNegociacao => "Em Negociação",
            StatusLead.Cliente => "Cliente",
            StatusLead.SemInteresse => "Sem Interesse",
            _ => status.ToString()
        };
    }

    public static string ObterCorHex(this StatusLead status)
    {
        return status switch
        {
            StatusLead.Novo => "#3B82F6",          // Blue
            StatusLead.Interessado => "#8B5CF6",   // Purple
            StatusLead.Contatado => "#F59E0B",     // Amber
            StatusLead.EmNegociacao => "#EC4899",  // Pink
            StatusLead.Cliente => "#10B981",       // Emerald
            StatusLead.SemInteresse => "#6B7280",   // Gray
            _ => "#6366F1"                         // Default Indigo
        };
    }
}
