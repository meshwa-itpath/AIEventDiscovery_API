using AIEventDiscovery.Constants;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AIEventDiscovery.Helpers;

/// <summary>
/// Builds semantic query texts and metadata filters for the recommendation pipeline.
/// Applies technology taxonomy to anchor queries to primary ecosystems and
/// avoid vague modifier-only queries that cause off-topic results.
/// </summary>
public static class RecommendationQueryBuilder
{
    // ── Public Classification API ────────────────────────────────────────────────
    // Taxonomy data lives in TechnologyTaxonomy (Constants/EventConstants.cs).
    // Update that class when adding or renaming technology options in the UI.

    public static bool IsPrimaryEcosystem(string technology)
        => TechnologyTaxonomy.PrimaryEcosystems.Contains(technology);

    public static bool IsCrossCuttingModifier(string technology)
        => TechnologyTaxonomy.CrossCuttingModifiers.Contains(technology);

    /// <summary>
    /// Resolves primary ecosystems and modifier interests from a user profile.
    /// If the user has structured PrimaryStacks set, uses them directly as primaries and Interests as modifiers.
    /// Otherwise falls back to classifying the legacy Technology string.
    /// </summary>
    public static (List<string> Primaries, List<string> Modifiers, List<string> AllTechnologies) ResolveUserPreferences(User user)
    {
        var primaryStacks = ParseList(user.PrimaryStacks);
        var interests = ParseList(user.Interests);

        if (primaryStacks.Count > 0 || interests.Count > 0)
        {
            var all = new List<string>(primaryStacks);
            all.AddRange(interests);

            // If user only selected interests with no primary stack, treat interests as primaries
            var effectivePrimaries = primaryStacks.Count > 0 ? primaryStacks : interests;
            var effectiveModifiers = primaryStacks.Count > 0 ? interests : [];

            return (effectivePrimaries, effectiveModifiers, all);
        }

        // Fallback to legacy Technology field
        var legacyTech = ParseList(user.Technology);
        var (primaries, modifiers) = ClassifyTechnologies(legacyTech);
        return (primaries, modifiers, legacyTech);
    }

    private static List<string> ParseList(string? value) =>
        value?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    /// <summary>
    /// Classifies a user's technology list into primary ecosystems and cross-cutting modifiers.
    /// </summary>
    public static (List<string> Primaries, List<string> Modifiers) ClassifyTechnologies(List<string> technologies)
    {
        var primaries = technologies.Where(IsPrimaryEcosystem).ToList();
        var modifiers = technologies.Where(IsCrossCuttingModifier).ToList();

        // If all selected techs are modifiers (no primary), treat them all as primaries
        // so the pipeline can still issue queries for each one.
        if (primaries.Count == 0)
        {
            primaries = technologies.ToList();
        }

        return (primaries, modifiers);
    }

    // ── Query Builder Methods ────────────────────────────────────────────────────

    /// <summary>
    /// Builds an anchored semantic query for a primary ecosystem, explicitly prioritizing the
    /// primary technology as the core relevance domain, with modifier technologies (e.g., API Development, CI/CD)
    /// framed as secondary topics within that primary ecosystem.
    /// </summary>
    public static string BuildAnchoredQuery(User user, string primaryTech, List<string> modifiers)
    {
        if (modifiers.Count == 0)
        {
            return $"Technical events and conferences for a {user.Role} specializing in {primaryTech}. Core focus is {primaryTech}.";
        }

        var modifierString = string.Join(", ", modifiers);
        return $"Technical events and conferences for a {user.Role} specializing primarily in {primaryTech}. " +
               $"Core topic must be {primaryTech}, with secondary interest in {modifierString}. " +
               $"Focus on {primaryTech} development, architecture, tools, and best practices relating to {modifierString}.";
    }

    /// <summary>
    /// Builds a fallback semantic query when no technologies are set on the user profile.
    /// </summary>
    public static string BuildGenericQuery(User user)
        => $"Find technical events suitable for a {user.Role} interested in general technical concepts";

    // ── Filter Builder ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds hard metadata pre-filters (level, mode) to apply in the pgvector SQL query.
    /// Only binary / strict fields belong here — not technology interests.
    /// </summary>
    public static EventQueryFilters? BuildQueryFilters(List<string>? levels = null, string? mode = null)
    {
        var filters = new EventQueryFilters();
        bool hasFilter = false;

        var validLevels = levels?
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        if (validLevels?.Count > 0)
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
