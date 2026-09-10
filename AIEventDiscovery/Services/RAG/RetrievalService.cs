using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Embeddings;
using AIEventDiscovery.Services.Interfaces;
using AIEventDiscovery.Services.PgVector;

namespace AIEventDiscovery.Services.RAG;

public class RetrievalService : IRetrievalService
{
    private readonly IPgVectorService _pgVectorService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<RetrievalService> _logger;

    public RetrievalService(
        IPgVectorService pgVectorService,
        IEmbeddingService embeddingService,
        IGeminiService geminiService,
        ILogger<RetrievalService> logger)
    {
        _pgVectorService = pgVectorService;
        _embeddingService = embeddingService;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<List<RecommendedEventDto>> ExecutePipelineAsync(RetrievalRequest request)
    {
        // 1. Generate Query Embedding
        var queryEmbedding = _embeddingService.GenerateEmbedding(request.QueryText);

        // 2. Query pgvector with optional metadata filters
        var rawEvents = await QueryPgVectorAsync(queryEmbedding, request);

        // 3. Apply Similarity Score Threshold
        var filteredByScore = rawEvents.Where(e => e.SimilarityScore >= request.SimilarityThreshold).ToList();

        // 4. Remove Expired Events
        var currentEvents = FilterExpiredEvents(filteredByScore, request.FilterExpiredEvents);

        // 5. Remove Duplicates
        var uniqueEvents = currentEvents.GroupBy(e => e.Id).Select(g => g.First()).ToList();

        // 6. Re-ranking (based on Date + Score)
        var rankedEvents = ReRankEvents(uniqueEvents, request.EnableReRanking, request.FinalLimit);

        // 7. Gemini Explanation Generation
        if (request.EnableGeminiExplanation && request.UserContext != null)
        {
            await GenerateExplanationsAsync(rankedEvents, request.UserContext);
        }

        return rankedEvents;
    }

    private async Task<List<RecommendedEventDto>> QueryPgVectorAsync(float[] queryEmbedding, RetrievalRequest request)
    {
        List<(Entities.Event Event, double SimilarityScore)> results;
        try
        {
            results = await _pgVectorService.QuerySimilarAsync(queryEmbedding, request.Limit, request.QueryFilters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "pgvector query failed.");
            return [];
        }

        if (results.Count == 0)
        {
            _logger.LogWarning("pgvector returned no results for the given query.");
            return [];
        }

        // Map Event entities → RecommendedEventDto
        return results.Select(r => new RecommendedEventDto
        {
            Id             = r.Event.Id.ToString(),
            Title          = r.Event.Title,
            Description    = r.Event.Description,
            Category       = r.Event.Category,
            SubCategory    = r.Event.SubCategory,
            Technologies   = r.Event.Technologies,
            Tags           = r.Event.Tags,
            Organizer      = r.Event.Organizer,
            City           = r.Event.City,
            Country        = r.Event.Country,
            Venue          = r.Event.Venue,
            Mode           = r.Event.Mode,
            Level          = r.Event.Level,
            EventType      = r.Event.EventType,
            StartDate      = r.Event.StartDate,
            EndDate        = r.Event.EndDate,
            Rating         = r.Event.Rating,
            SimilarityScore = r.SimilarityScore
        }).ToList();
    }

    private static List<RecommendedEventDto> FilterExpiredEvents(List<RecommendedEventDto> events, bool filterExpired)
    {
        if (!filterExpired) return events;
        var today = DateTime.UtcNow.Date;
        return events.Where(e => e.StartDate == null || e.StartDate >= today).ToList();
    }

    private static List<RecommendedEventDto> ReRankEvents(List<RecommendedEventDto> events, bool enableReRanking, int finalLimit)
    {
        if (!enableReRanking)
            return events.Take(finalLimit).ToList();

        var today = DateTime.UtcNow;
        return events.OrderByDescending(e =>
        {
            var daysAway = e.StartDate.HasValue ? (e.StartDate.Value - today).TotalDays : 365;
            if (daysAway < 0) daysAway = 365;
            double timeDecay = 1.0 / (1.0 + Math.Log10(Math.Max(1, daysAway) + 1));
            return e.SimilarityScore * timeDecay;
        }).Take(finalLimit).ToList();
    }

    private async Task GenerateExplanationsAsync(List<RecommendedEventDto> events, Entities.User user)
    {
        var explanations = await _geminiService.GenerateBatchExplanationsAsync(user, events);
        foreach (var ev in events)
        {
            if (explanations.TryGetValue(ev.Id, out var explanation))
                ev.Explanation = explanation;
        }
    }
}
