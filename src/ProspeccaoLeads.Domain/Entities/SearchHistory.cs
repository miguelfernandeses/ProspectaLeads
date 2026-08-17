namespace ProspeccaoLeads.Domain.Entities;

public class SearchHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Niche { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
