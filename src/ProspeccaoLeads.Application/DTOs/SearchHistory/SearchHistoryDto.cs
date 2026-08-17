namespace ProspeccaoLeads.Application.DTOs.SearchHistory;

public class SearchHistoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Niche { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
