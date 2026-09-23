using AIEventDiscovery.Entities;

namespace AIEventDiscovery.DTOs;

public class RetrievalRequest
{
    public string QueryText { get; set; } = string.Empty;
    public EventQueryFilters? QueryFilters { get; set; }
    public int Limit { get; set; } = 20;
    public double SimilarityThreshold { get; set; } = 0.7;
    public bool EnableReRanking { get; set; } = false;
    public bool FilterExpiredEvents { get; set; } = false;
    public bool EnableGeminiExplanation { get; set; } = false;
    public User? UserContext { get; set; }
    public List<MetadataFilterItem>? SoftFilters { get; set; }
}
