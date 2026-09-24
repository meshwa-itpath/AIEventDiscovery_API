using System.Text.Json.Serialization;

namespace AIEventDiscovery.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MetadataFilterType
{
    Hard,
    Soft
}

public class QueryUnderstandingResult
{
    public bool IsSuccess { get; set; } = true;
    public string MainQuery { get; set; } = string.Empty;
    public List<MetadataFilterItem> Metadata { get; set; } = new();
}

public class MetadataFilterItem
{
    public string Field { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
    public MetadataFilterType Type { get; set; } = MetadataFilterType.Hard;
}
