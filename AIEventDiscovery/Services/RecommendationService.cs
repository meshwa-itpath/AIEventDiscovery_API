using AIEventDiscovery.Data.Repositories;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;
using AIEventDiscovery.Helpers;
using AIEventDiscovery.Services.Interfaces;

namespace AIEventDiscovery.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRetrievalService _retrievalService;

    public RecommendationService(
        IGenericRepository<User> userRepository,
        ICurrentUserService currentUserService,
        IRetrievalService retrievalService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _retrievalService = retrievalService;
    }

    public async Task<ApiResponse<List<RecommendedEventDto>>> GetRecommendedEventsAsync(
        int limit,
        string? level = null,
        string? mode = null)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return ApiResponse<List<RecommendedEventDto>>.Fail("User is not authenticated.");
        }

        var user = await _userRepository.GetByIdAsync<User>(userId);
        if (user == null)
        {
            return ApiResponse<List<RecommendedEventDto>>.Fail("User details not found.");
        }

        if (!user.IsOnBoardingCompleted)
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok([], "Please complete your onboarding details to get recommendations.");
        }

        var queryText = RecommendationQueryBuilder.BuildSemanticQuery(user);
        var metadataFilter = RecommendationQueryBuilder.BuildMetadataFilters(level, mode);

        var request = new RetrievalRequest
        {
            QueryText = queryText,
            MetadataWhereFilter = metadataFilter,
            Limit = 20, 
            FinalLimit = limit,
            SimilarityThreshold = 0.5,
            EnableReRanking = true,
            FilterExpiredEvents = true,
            EnableGeminiExplanation = true,
            UserContext = user
        };

        var results = await _retrievalService.ExecutePipelineAsync(request);

        if (!results.Any())
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok(new List<RecommendedEventDto>(), "No matching events found.");
        }

        return ApiResponse<List<RecommendedEventDto>>.Ok(results, "Successfully retrieved recommended events.");
    }

    public async Task<ApiResponse<List<RecommendedEventDto>>> SearchEventsAsync(string query, int limit, string? level = null, string? mode = null)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return ApiResponse<List<RecommendedEventDto>>.Fail("User is not authenticated.");
        }

        var user = await _userRepository.GetByIdAsync<User>(userId);
        if (user == null)
        {
            return ApiResponse<List<RecommendedEventDto>>.Fail("User details not found.");
        }

        if (level == "All") level = null;

        var metadataFilter = RecommendationQueryBuilder.BuildMetadataFilters(level, mode);

        var request = new RetrievalRequest
        {
            QueryText = query,
            MetadataWhereFilter = metadataFilter,
            Limit = 20, 
            FinalLimit = limit,
            SimilarityThreshold = 0.5,
            EnableReRanking = true,
            FilterExpiredEvents = true,
            EnableGeminiExplanation = true,
            UserContext = user
        };

        var results = await _retrievalService.ExecutePipelineAsync(request);

        if (!results.Any())
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok(new List<RecommendedEventDto>(), "No matching events found.");
        }

        return ApiResponse<List<RecommendedEventDto>>.Ok(results, "Successfully retrieved search results.");
    }
}
