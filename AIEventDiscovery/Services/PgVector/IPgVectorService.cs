using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;

namespace AIEventDiscovery.Services.PgVector;

public interface IPgVectorService
{
    /// <summary>
    /// Upserts a batch of events (with their embeddings) into the Events table.
    /// Existing rows (matched by Title + StartDate) are skipped to avoid full re-seed overhead.
    /// </summary>
    Task<int> UpsertEventsAsync(IEnumerable<Event> events);

    /// <summary>
    /// Returns the top <paramref name="limit"/> events ordered by cosine similarity
    /// to the given query embedding, with optional server-side filters.
    /// Each result is annotated with a SimilarityScore (0–1, higher = more similar).
    /// </summary>
    Task<List<(Event Event, double SimilarityScore)>> QuerySimilarAsync(
        float[] queryEmbedding,
        int limit,
        EventQueryFilters? filters = null);

    /// <summary>
    /// Removes all events from the Events table.
    /// </summary>
    Task DeleteAllEventsAsync();
}

