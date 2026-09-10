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
        if (!string.IsNullOrWhiteSpace(filters?.Category))
            query = query.Where(e => e.Category == filters.Category);

        if (!string.IsNullOrWhiteSpace(filters?.City))
            query = query.Where(e => e.City == filters.City);

        if (!string.IsNullOrWhiteSpace(filters?.Country))
            query = query.Where(e => e.Country == filters.Country);

        if (!string.IsNullOrWhiteSpace(filters?.Mode))
        {
            var targetMode = filters.Mode.Trim().ToLower();
            query = query.Where(e => e.Mode != null && e.Mode.ToLower() == targetMode);
        }

        if (filters?.Levels != null && filters.Levels.Any())
        {
            var targetLevels = filters.Levels
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim().ToLower())
                .ToList();
            targetLevels.Add("all levels");
            targetLevels.Add("all");

            query = query.Where(e => e.Level != null && targetLevels.Contains(e.Level.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filters?.EventType))
            query = query.Where(e => e.EventType == filters.EventType);

        if (filters?.StartDateFrom.HasValue == true)
            query = query.Where(e => e.StartDate >= filters.StartDateFrom);

        if (filters?.StartDateTo.HasValue == true)
            query = query.Where(e => e.StartDate <= filters.StartDateTo);

        // Order by cosine distance (ascending = most similar first) and take top N
        var rawResults = await query
            .OrderBy(e => e.Embedding!.CosineDistance(queryVector))
            .Take(limit)
            .Select(e => new
            {
                Event = e,
                Distance = e.Embedding!.CosineDistance(queryVector)
            })
            .ToListAsync();

        // Convert cosine distance → similarity score (0–1, 1 = identical)
        return rawResults
            .Select(r => (r.Event, SimilarityScore: Math.Round(1.0 - r.Distance, 4)))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task DeleteAllEventsAsync()
    {
        var deleted = await _db.Events.ExecuteDeleteAsync();
        _logger.LogInformation("Deleted {Count} events from the Events table.", deleted);
    }
}
