using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AIEventDiscovery.DTOs;

public class ChromaQueryResponse
{
    [JsonPropertyName("ids")]
    public List<List<string>>? Ids { get; set; }

    [JsonPropertyName("distances")]
    public List<List<double>>? Distances { get; set; }

    [JsonPropertyName("metadatas")]
    public List<List<Dictionary<string, string>>>? Metadatas { get; set; }

    [JsonPropertyName("documents")]
    public List<List<string>>? Documents { get; set; }
}
