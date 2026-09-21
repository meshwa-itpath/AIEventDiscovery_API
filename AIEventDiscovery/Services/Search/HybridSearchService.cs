using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Interfaces;

namespace AIEventDiscovery.Services.Search;

public class HybridSearchService : IHybridSearchService
{
    private readonly IGeminiService _geminiService;
    private readonly IRetrievalService _retrievalService;

    public HybridSearchService(IGeminiService geminiService, IRetrievalService retrievalService)
    {
        _geminiService = geminiService;
        _retrievalService = retrievalService;
    }

    public async Task<ApiResponse<List<RecommendedEventDto>>> SearchEventsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        // 1. Understand Query using Gemini
        var understanding = await _geminiService.UnderstandQueryAsync(query, cancellationToken);
        var mainQuery = string.IsNullOrWhiteSpace(understanding.MainQuery) ? query : understanding.MainQuery;

        // 2. Separate Hard and Soft filters
        var hardFilters = understanding.Metadata.Where(m => m.Type == MetadataFilterType.Hard).ToList();
        var softFilters = understanding.Metadata.Where(m => m.Type == MetadataFilterType.Soft).ToList();

        // 3. Map Hard filters to EventQueryFilters for PostgreSQL WHERE clause
        var eventQueryFilters = new EventQueryFilters();
        
        foreach (var filter in hardFilters)
        {
            if (filter.Values == null || !filter.Values.Any()) continue;

            switch (filter.Field.ToLower())
            {
                case "country": eventQueryFilters.Countries = (eventQueryFilters.Countries ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "city": eventQueryFilters.Cities = (eventQueryFilters.Cities ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "venue": eventQueryFilters.Venues = (eventQueryFilters.Venues ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "technology": eventQueryFilters.Technologies = (eventQueryFilters.Technologies ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "mode": eventQueryFilters.Modes = (eventQueryFilters.Modes ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "level": eventQueryFilters.Levels = (eventQueryFilters.Levels ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "eventtype": eventQueryFilters.EventTypes = (eventQueryFilters.EventTypes ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "category": eventQueryFilters.Categories = (eventQueryFilters.Categories ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "subcategory": eventQueryFilters.SubCategories = (eventQueryFilters.SubCategories ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "organizer": eventQueryFilters.Organizers = (eventQueryFilters.Organizers ?? new List<string>()).Concat(filter.Values).ToList(); break;
                case "tags": eventQueryFilters.Tags = (eventQueryFilters.Tags ?? new List<string>()).Concat(filter.Values).ToList(); break;
            }
        }

        // 4. Create unified RetrievalRequest
        var retrievalRequest = new RetrievalRequest
        {
            QueryText = mainQuery,
            QueryFilters = eventQueryFilters,
            SoftFilters = softFilters,
            Limit = limit * 2, // Fetch more candidates for re-ranking
            FinalLimit = limit,
            SimilarityThreshold = 0.5, // slightly lower threshold to allow soft boost to bring up relevant ones
            EnableReRanking = true,
            FilterExpiredEvents = true
        };

        // 5. Execute retrieval pipeline
        var results = await _retrievalService.ExecutePipelineAsync(retrievalRequest);

        if (results.Count == 0)
            return ApiResponse<List<RecommendedEventDto>>.Fail("No events found matching your search. Try a different query.");

        return ApiResponse<List<RecommendedEventDto>>.Paginated(
            results,
            totalRecords: results.Count,
            page: 1,
            pageSize: limit,
            message: $"Found {results.Count} events matching your search.");
    }
}
