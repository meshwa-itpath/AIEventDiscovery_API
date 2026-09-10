using System.Collections.Generic;
using System.Linq;
using AIEventDiscovery.DTOs;
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

    public static string BuildSemanticQueryForTech(User user, string technology)
    {
        return $"Find technical events suitable for {user.Role} interested in {technology}";
    }


    public static EventQueryFilters? BuildQueryFilters(List<string>? levels = null, string? mode = null)
    {
        bool hasFilter = false;
        var filters = new EventQueryFilters();

        var validLevels = levels?
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        if (validLevels != null && validLevels.Any())
        {
            filters.Levels = validLevels;
            hasFilter = true;
        }

        if (!string.IsNullOrWhiteSpace(mode))
        {
            filters.Mode = mode.Trim();
            hasFilter = true;
        }

        return hasFilter ? filters : null;
    }
}
