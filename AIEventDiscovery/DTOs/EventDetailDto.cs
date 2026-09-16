namespace AIEventDiscovery.DTOs;

/// <summary>
/// Data transfer object for full event details view.
/// Excludes pgvector embedding payload for optimal network performance.
/// </summary>
public class EventDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public List<string> Technologies { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string? Organizer { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Venue { get; set; }
    public string? Mode { get; set; }
    public string? Level { get; set; }
    public string? EventType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double? Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
