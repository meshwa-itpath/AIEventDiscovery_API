using System;
using System.Collections.Generic;

namespace AIEventDiscovery.DTOs;

public class RecommendedEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public List<string> Technologies { get; set; } = new();
    public List<string> Tags { get; set; } = new();
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
    public double SimilarityScore { get; set; }
    public string? Explanation { get; set; }
}
