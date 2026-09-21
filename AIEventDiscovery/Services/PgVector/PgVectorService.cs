using AIEventDiscovery.DTOs;
using AIEventDiscovery.Data;
using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AIEventDiscovery.Services.PgVector;

public class PgVectorService : IPgVectorService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PgVectorService> _logger;

    public PgVectorService(ApplicationDbContext db, ILogger<PgVectorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> UpsertEventsAsync(IEnumerable<Event> events)
    {
        var list = events.ToList();
        if (list.Count == 0) return 0;

        // Add all events; duplicate detection can be handled by the caller
        // or by unique constraints on the table if needed.
        await _db.Events.AddRangeAsync(list);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Upserted {Count} events into pgvector Events table.", list.Count);
        return list.Count;
    }

    /// <inheritdoc/>
    public async Task<List<(Event Event, double SimilarityScore)>> QuerySimilarAsync(
        float[] queryEmbedding,
        int limit,
        EventQueryFilters? filters = null)
    {
        var queryVector = new Vector(queryEmbedding);

        // Start with base query, excluding soft-deleted rows
        var query = _db.Events
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        // Apply optional server-side pre-filters before the ANN scan
        if (filters != null)
        {
            query = ApplyFilters(query, filters);
        }

        // Order by cosine distance (ascending = most similar first) and take top N
        var rawResults = await query
            .Select(e => new
            {
                Event = e,
                Distance = e.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync();

        // Convert cosine distance → similarity score (0–1, 1 = identical)
        return rawResults
            .Select(r => (r.Event, SimilarityScore: Math.Round(1.0 - r.Distance, 4)))
            .ToList();
    }

    private IQueryable<Event> ApplyFilters(IQueryable<Event> query, EventQueryFilters filters)
    {
        if (filters.Categories?.Any() == true)
        {
            var targets = filters.Categories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.Category != null && targets.Contains(e.Category));
        }

        if (filters.SubCategories?.Any() == true)
        {
            var targets = filters.SubCategories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.SubCategory != null && targets.Contains(e.SubCategory));
        }

        if (filters.Cities?.Any() == true)
        {
            var targets = filters.Cities.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.City != null && targets.Contains(e.City));
        }

        if (filters.Countries?.Any() == true)
        {
            var targets = filters.Countries.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.Country != null && targets.Contains(e.Country));
        }

        if (filters.Venues?.Any() == true)
        {
            var targets = filters.Venues.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.Venue != null && targets.Contains(e.Venue));
        }

        if (filters.Modes?.Any() == true)
        {
            var targets = filters.Modes.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.Mode != null && targets.Contains(e.Mode));
        }

        if (filters.Levels?.Any() == true)
        {
            var targets = filters.Levels.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();
            targets.Add("All Levels");
            targets.Add("All");

            if (targets.Any())
                query = query.Where(e => e.Level != null && targets.Contains(e.Level));
        }

        if (filters.EventTypes?.Any() == true)
        {
            var targets = filters.EventTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.EventType != null && targets.Contains(e.EventType));
        }

        if (filters.Organizers?.Any() == true)
        {
            var targets = filters.Organizers.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();
            if (targets.Any())
                query = query.Where(e => e.Organizer != null && targets.Contains(e.Organizer));
        }

        // Removed Technologies and Tags from DB hard-filtering due to Npgsql EF Core translation incompatibilities.
        // Semantic Vector Search will naturally fetch relevant technologies, and we will apply soft-boosting in RetrievalService.

        if (filters.StartDateFrom.HasValue)
            query = query.Where(e => e.StartDate >= filters.StartDateFrom);

        if (filters.StartDateTo.HasValue)
            query = query.Where(e => e.StartDate <= filters.StartDateTo);
            
        if (filters.EndDateFrom.HasValue)
            query = query.Where(e => e.EndDate >= filters.EndDateFrom);

        if (filters.EndDateTo.HasValue)
            query = query.Where(e => e.EndDate <= filters.EndDateTo);
            
        if (filters.MinRating.HasValue)
            query = query.Where(e => e.Rating >= filters.MinRating);

        return query;
    }

    /// <inheritdoc/>
    public async Task DeleteAllEventsAsync()
    {
        var deleted = await _db.Events.ExecuteDeleteAsync();
        _logger.LogInformation("Deleted {Count} events from the Events table.", deleted);
    }
}
