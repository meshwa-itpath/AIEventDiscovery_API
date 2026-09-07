using System.Text.Json;
using AIEventDiscovery.Configuration;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Embeddings;
using AIEventDiscovery.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AIEventDiscovery.Services.RAG;

public class RetrievalService : IRetrievalService
{
    private readonly IChromaService _chromaService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGeminiService _geminiService;
    private readonly ChromaDbOptions _chromaDbOptions;
    private readonly ILogger<RetrievalService> _logger;

    public RetrievalService(
        IChromaService chromaService,
        IEmbeddingService embeddingService,
        IGeminiService geminiService,
        IOptions<ChromaDbOptions> chromaDbOptions,
        ILogger<RetrievalService> logger)
    {
        _chromaService = chromaService;
        _embeddingService = embeddingService;
        _geminiService = geminiService;
        _chromaDbOptions = chromaDbOptions.Value;
        _logger = logger;
    }

    public async Task<List<RecommendedEventDto>> ExecutePipelineAsync(RetrievalRequest request)
    {
        // 1. Generate Query Embedding
        var queryEmbedding = GenerateQueryEmbedding(request.QueryText);

        // 2. Query ChromaDB with Metadata Filtering
        var rawEvents = await QueryChromaDbAsync(queryEmbedding, request);

        // 3. Apply Similarity Score Threshold
        var filteredByScore = ApplySimilarityThreshold(rawEvents, request.SimilarityThreshold);

        // 4. Remove Expired Events
        var currentEvents = FilterExpiredEvents(filteredByScore, request.FilterExpiredEvents);

        // 5. Remove Duplicates
        var uniqueEvents = RemoveDuplicates(currentEvents);

        // 6. Re-ranking (Top-20 -> Top-10 based on Date + Score)
        var rankedEvents = ReRankEvents(uniqueEvents, request.EnableReRanking, request.FinalLimit);

        // 7. Gemini Explanation Generation
        if (request.EnableGeminiExplanation && request.UserContext != null)
        {
            await GenerateExplanationsAsync(rankedEvents, request.UserContext);
        }

        return rankedEvents;
    }

    private float[] GenerateQueryEmbedding(string queryText)
    {
        return _embeddingService.GenerateEmbedding(queryText);
    }

    private async Task<List<RecommendedEventDto>> QueryChromaDbAsync(float[] queryEmbedding, RetrievalRequest request)
    {
        var collectionResponse = await _chromaService.GetCollectionAsync(_chromaDbOptions.CollectionName);
        if (!collectionResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Collection '{CollectionName}' not found in ChromaDB.", _chromaDbOptions.CollectionName);
            return new List<RecommendedEventDto>();
        }

        var collectionJson = await collectionResponse.Content.ReadAsStringAsync();
        using var collectionDoc = JsonDocument.Parse(collectionJson);
        var collectionId = collectionDoc.RootElement.GetProperty("id").GetString()!;

        var queryPayload = new Dictionary<string, object>
        {
            { "query_embeddings", new[] { queryEmbedding } },
            { "n_results", request.Limit }
        };

        if (request.MetadataWhereFilter != null)
        {
            queryPayload.Add("where", request.MetadataWhereFilter);
        }

        if (request.DocumentWhereFilter != null)
        {
            queryPayload.Add("where_document", request.DocumentWhereFilter);
        }

        var queryResponse = await _chromaService.QueryAsync(collectionId, queryPayload);
        if (!queryResponse.IsSuccessStatusCode)
        {
            _logger.LogError("ChromaDB query failed: {Error}", await queryResponse.Content.ReadAsStringAsync());
            return new List<RecommendedEventDto>();
        }

        var responseJson = await queryResponse.Content.ReadAsStringAsync();
        var rawResponse = JsonSerializer.Deserialize<ChromaQueryResponse>(responseJson);

        if (rawResponse?.Ids == null || !rawResponse.Ids.Any() || !rawResponse.Ids[0].Any())
        {
            return new List<RecommendedEventDto>();
        }

        var results = new List<RecommendedEventDto>();
        var ids = rawResponse.Ids[0];
        var distances = rawResponse.Distances?[0];
        var metadatas = rawResponse.Metadatas?[0];
        var documents = rawResponse.Documents?[0];

        for (int i = 0; i < ids.Count; i++)
        {
            var metadata = metadatas?[i] ?? new Dictionary<string, string>();

            var eventDto = new RecommendedEventDto
            {
                Id = ids[i],
                Title = metadata.GetValueOrDefault("title", string.Empty),
                Description = documents?[i] ?? string.Empty,
                Category = metadata.GetValueOrDefault("category"),
                SubCategory = metadata.GetValueOrDefault("subCategory"),
                Organizer = metadata.GetValueOrDefault("organizer"),
                City = metadata.GetValueOrDefault("city"),
                Country = metadata.GetValueOrDefault("country"),
                Venue = metadata.GetValueOrDefault("venue"),
                Mode = metadata.GetValueOrDefault("mode"),
                Level = metadata.GetValueOrDefault("level"),
                EventType = metadata.GetValueOrDefault("eventType"),
                Rating = double.TryParse(metadata.GetValueOrDefault("rating"), out var r) ? r : null,
                StartDate = DateTime.TryParse(metadata.GetValueOrDefault("startDate"), out var sd) ? sd : null,
                EndDate = DateTime.TryParse(metadata.GetValueOrDefault("endDate"), out var ed) ? ed : null,
                SimilarityScore = distances != null ? Math.Round(1.0 - distances[i], 4) : 0.0
            };

            if (metadata.TryGetValue("technologies", out var techs) && !string.IsNullOrWhiteSpace(techs))
            {
                eventDto.Technologies = techs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            if (metadata.TryGetValue("tags", out var tags) && !string.IsNullOrWhiteSpace(tags))
            {
                eventDto.Tags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            results.Add(eventDto);
        }

        return results;
    }

    private List<RecommendedEventDto> ApplySimilarityThreshold(List<RecommendedEventDto> events, double threshold)
    {
        return events.Where(e => e.SimilarityScore >= threshold).ToList();
    }

    private List<RecommendedEventDto> FilterExpiredEvents(List<RecommendedEventDto> events, bool filterExpired)
    {
        if (!filterExpired) return events;
        var today = DateTime.UtcNow.Date;
        return events.Where(e => e.StartDate == null || e.StartDate >= today).ToList();
    }

    private List<RecommendedEventDto> RemoveDuplicates(List<RecommendedEventDto> events)
    {
        return events.GroupBy(e => e.Id).Select(g => g.First()).ToList();
    }

    private List<RecommendedEventDto> ReRankEvents(List<RecommendedEventDto> events, bool enableReRanking, int finalLimit)
    {
        if (!enableReRanking)
        {
            return events.Take(finalLimit).ToList();
        }

        var today = DateTime.UtcNow;
        var ranked = events.OrderByDescending(e => 
        {
            var daysAway = e.StartDate.HasValue ? (e.StartDate.Value - today).TotalDays : 365;
            if (daysAway < 0) daysAway = 365;
            double timeDecay = 1.0 / (1.0 + Math.Log10(Math.Max(1, daysAway) + 1));
            return e.SimilarityScore * timeDecay;
        }).Take(finalLimit).ToList();

        return ranked;
    }

    private async Task GenerateExplanationsAsync(List<RecommendedEventDto> events, Entities.User user)
    {
        var explanations = await _geminiService.GenerateBatchExplanationsAsync(user, events);
        foreach (var ev in events)
        {
            if (explanations.TryGetValue(ev.Id, out var explanation))
            {
                ev.Explanation = explanation;
            }
        }
    }
}
