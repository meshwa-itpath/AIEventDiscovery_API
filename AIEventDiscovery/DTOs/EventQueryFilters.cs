namespace AIEventDiscovery.DTOs;

/// <summary>
/// Optional server-side filters applied before the vector similarity search.
/// </summary>
public class EventQueryFilters
{
    public string? Category { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Mode { get; set; }
    public List<string>? Levels { get; set; }
    public string? EventType { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
}
