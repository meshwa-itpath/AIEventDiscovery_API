using System.Collections.Generic;
using System.Linq;
using AIEventDiscovery.Entities;

namespace AIEventDiscovery.Helpers;

public static class RecommendationQueryBuilder
{
    public static string BuildSemanticQuery(User user)
    {
        var technologies = user.Technology?
            .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
            .ToList() ?? new List<string>();

        var techString = technologies.Any() ? string.Join(", ", technologies) : "general technical concepts";
        
        return $"Find technical events suitable for {user.Role} interested in {techString}";
    }

    public static object? BuildMetadataFilters(string? level = null, string? mode = null)
    {
        var metadataFilters = new List<Dictionary<string, object>>();

        if (!string.IsNullOrWhiteSpace(level))
        {
            metadataFilters.Add(new Dictionary<string, object> { { "level", level } });
        }

        if (!string.IsNullOrWhiteSpace(mode))
        {
            metadataFilters.Add(new Dictionary<string, object> { { "mode", mode } });
        }

        if (metadataFilters.Any())
        {
            if (metadataFilters.Count == 1)
            {
                return metadataFilters[0];
            }
            else
            {
                return new Dictionary<string, object>
                {
                    { "$and", metadataFilters.ToArray() }
                };
            }
        }

        return null;
    }
}
