namespace AIEventDiscovery.DTOs;

/// <summary>
/// Server-side filters applied in the PostgreSQL WHERE clause before the vector similarity search.
/// Supports single and multi-value constraints across all Event properties.
/// </summary>
public class EventQueryFilters
{
    // Technologies
    public List<string>? Technologies { get; set; }

    // Categories
    public List<string>? Categories { get; set; }
    public string? Category
    {
        get => Categories?.FirstOrDefault();
        set => Categories = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // SubCategories
    public List<string>? SubCategories { get; set; }
    public string? SubCategory
    {
        get => SubCategories?.FirstOrDefault();
        set => SubCategories = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // Tags
    public List<string>? Tags { get; set; }

    // Organizers
    public List<string>? Organizers { get; set; }
    public string? Organizer
    {
        get => Organizers?.FirstOrDefault();
        set => Organizers = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // Cities
    public List<string>? Cities { get; set; }
    public string? City
    {
        get => Cities?.FirstOrDefault();
        set => Cities = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // Countries
    public List<string>? Countries { get; set; }
    public string? Country
    {
        get => Countries?.FirstOrDefault();
        set => Countries = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // Venues
    public List<string>? Venues { get; set; }
    public string? Venue
    {
        get => Venues?.FirstOrDefault();
        set => Venues = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // Modes (Online, In-Person, Hybrid)
    public List<string>? Modes { get; set; }
    public string? Mode
    {
        get => Modes?.FirstOrDefault();
        set => Modes = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // Levels (Beginner, Intermediate, Advanced, All Levels)
    public List<string>? Levels { get; set; }
    public string? Level
    {
        get => Levels?.FirstOrDefault();
        set => Levels = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // EventTypes (Conference, Workshop, Meetup, Webinar, etc.)
    public List<string>? EventTypes { get; set; }
    public string? EventType
    {
        get => EventTypes?.FirstOrDefault();
        set => EventTypes = string.IsNullOrWhiteSpace(value) ? null : [value];
    }

    // Dates
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public DateTime? EndDateFrom { get; set; }
    public DateTime? EndDateTo { get; set; }

    // Rating
    public double? MinRating { get; set; }
}
