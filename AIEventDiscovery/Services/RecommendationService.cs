using AIEventDiscovery.Constants;
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
        int page,
        int pageSize,
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

        // Normalize filter inputs (treat "All" or whitespace as null)
        if (string.Equals(level, EventLevels.All, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(level))
        {
            level = null;
        }

        if (string.Equals(mode, "All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(mode))
        {
            mode = null;
        }

        var technologies = user.Technology?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? new List<string>();

        // Determine effective level filter vs proportional distribution:
        // 1. If explicit level was provided -> use explicit level for hard filter
        // 2. If no level provided and role is Student -> use Beginner for hard filter
        // 3. If no level provided and role is Professional -> no hard level filter in DB; apply proportional 70/15/15 blending
        bool isStudent = EventRoles.IsStudent(user.Role);
        List<string>? dbFilterLevels = null;
        bool applyProportionalBlending = false;

        if (!string.IsNullOrWhiteSpace(level))
        {
            dbFilterLevels = [level];
        }
        else if (isStudent)
        {
            dbFilterLevels = [EventLevels.Beginner];
        }
        else
        {
            applyProportionalBlending = true;
        }

        var queryFilters = RecommendationQueryBuilder.BuildQueryFilters(dbFilterLevels, mode);

        // Pool size = pageSize × 5, capped at 50 to cover all pages
        int poolSize = Math.Min(pageSize * 5, 50);

        List<RecommendedEventDto> fullRankedPool;

        if (!technologies.Any())
        {
            var queryText = RecommendationQueryBuilder.BuildSemanticQuery(user);
            var request = new RetrievalRequest
            {
                QueryText = queryText,
                QueryFilters = queryFilters,
                Limit = poolSize,
                FinalLimit = poolSize,
                SimilarityThreshold = 0.7,
                EnableReRanking = true,
                FilterExpiredEvents = true,
                EnableGeminiExplanation = false,
                UserContext = user
            };

            var rawPool = await _retrievalService.ExecutePipelineAsync(request);
            fullRankedPool = applyProportionalBlending 
                ? ApplyProportionalLevelDistribution(rawPool, poolSize)
                : rawPool;
        }
        else
        {
            var candidatePool = new List<(string Tech, RecommendedEventDto Event)>();
            int limitPerTech = Math.Max(15, (int)Math.Ceiling((double)poolSize / technologies.Count * 1.5));

            foreach (var tech in technologies)
            {
                var queryText = RecommendationQueryBuilder.BuildSemanticQueryForTech(user, tech);
                var request = new RetrievalRequest
                {
                    QueryText = queryText,
                    QueryFilters = queryFilters,
                    Limit = limitPerTech,
                    FinalLimit = limitPerTech,
                    SimilarityThreshold = 0.7,
                    EnableReRanking = true,
                    FilterExpiredEvents = true,
                    EnableGeminiExplanation = false,
                    UserContext = user
                };

                var techResults = await _retrievalService.ExecutePipelineAsync(request);
                foreach (var result in techResults)
                {
                    candidatePool.Add((tech, result));
                }
            }

            // Deduplicate: keep the entry with the highest similarity score per event
            var deduplicatedCandidates = new Dictionary<string, (string Tech, RecommendedEventDto Event)>();
            foreach (var candidate in candidatePool)
            {
                if (deduplicatedCandidates.TryGetValue(candidate.Event.Id, out var existingCandidate))
                {
                    if (candidate.Event.SimilarityScore > existingCandidate.Event.SimilarityScore)
                        deduplicatedCandidates[candidate.Event.Id] = candidate;
                }
                else
                {
                    deduplicatedCandidates[candidate.Event.Id] = candidate;
                }
            }

            // Group by interest, sorted by score within each group
            var groupedCandidates = deduplicatedCandidates.Values
                .GroupBy(c => c.Tech)
                .ToDictionary(g => g.Key, g => g.Select(c => c.Event).OrderByDescending(e => e.SimilarityScore).ToList());

            // Round Robin: pick one event per interest in turns
            var roundRobinPool = new List<RecommendedEventDto>();
            var pointers = technologies.ToDictionary(t => t, t => 0);
            bool addedAny = true;
            int maxPerInterest = Math.Max(10, (int)Math.Ceiling((double)poolSize / technologies.Count));

            while (addedAny && roundRobinPool.Count < poolSize)
            {
                addedAny = false;
                foreach (var tech in technologies)
                {
                    if (roundRobinPool.Count >= poolSize) break;

                    if (groupedCandidates.TryGetValue(tech, out var techEvents))
                    {
                        int pointer = pointers[tech];
                        if (pointer < techEvents.Count && pointer < maxPerInterest)
                        {
                            roundRobinPool.Add(techEvents[pointer]);
                            pointers[tech]++;
                            addedAny = true;
                        }
                    }
                }
            }

            // Apply proportional level blending if professional role and no explicit level was given
            fullRankedPool = applyProportionalBlending
                ? ApplyProportionalLevelDistribution(roundRobinPool, poolSize)
                : roundRobinPool.OrderByDescending(e => e.SimilarityScore).ToList();
        }

        if (!fullRankedPool.Any())
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok(new List<RecommendedEventDto>(), "No matching events found.");
        }

        // Apply pagination slice
        int totalRecords = fullRankedPool.Count;
        var pagedData = fullRankedPool
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return ApiResponse<List<RecommendedEventDto>>.Paginated(
            pagedData,
            totalRecords,
            page,
            pageSize,
            "Successfully retrieved recommended events.");
    }

    public async Task<ApiResponse<List<RecommendedEventDto>>> SearchEventsAsync(
        string query,
        int page,
        int pageSize,
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

        if (string.Equals(level, EventLevels.All, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(level))
        {
            level = null;
        }

        if (string.Equals(mode, "All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(mode))
        {
            mode = null;
        }

        var searchLevels = !string.IsNullOrWhiteSpace(level) ? new List<string> { level } : null;
        var queryFilters = RecommendationQueryBuilder.BuildQueryFilters(searchLevels, mode);

        // Pool size = pageSize × 5, capped at 50 to cover all pages
        int poolSize = Math.Min(pageSize * 5, 50);

        var request = new RetrievalRequest
        {
            QueryText = query,
            QueryFilters = queryFilters,
            Limit = poolSize,
            FinalLimit = poolSize,
            SimilarityThreshold = 0.7,
            EnableReRanking = true,
            FilterExpiredEvents = true,
            EnableGeminiExplanation = false,
            UserContext = user
        };

        var fullRankedPool = await _retrievalService.ExecutePipelineAsync(request);

        if (!fullRankedPool.Any())
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok(new List<RecommendedEventDto>(), "No matching events found.");
        }

        // Apply pagination slice
        int totalRecords = fullRankedPool.Count;
        var pagedData = fullRankedPool
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return ApiResponse<List<RecommendedEventDto>>.Paginated(
            pagedData,
            totalRecords,
            page,
            pageSize,
            "Successfully retrieved search results.");
    }

    /// <summary>
    /// Distributes candidates proportionally according to LevelDistributionRatios:
    /// ~70% Intermediate (+ All Levels / unclassified), ~15% Beginner, ~15% Advanced.
    /// If any bucket has fewer candidates than its quota, remaining slots are automatically
    /// backfilled from remaining candidates ordered by similarity score.
    /// </summary>
    private static List<RecommendedEventDto> ApplyProportionalLevelDistribution(
        List<RecommendedEventDto> candidates,
        int targetSize)
    {
        if (candidates.Count <= targetSize)
        {
            return candidates.OrderByDescending(e => e.SimilarityScore).ToList();
        }

        var intermediateBucket = new List<RecommendedEventDto>();
        var beginnerBucket = new List<RecommendedEventDto>();
        var advancedBucket = new List<RecommendedEventDto>();

        foreach (var ev in candidates)
        {
            var lvl = ev.Level?.Trim().ToLowerInvariant();
            if (lvl == "beginner")
            {
                beginnerBucket.Add(ev);
            }
            else if (lvl == "advanced")
            {
                advancedBucket.Add(ev);
            }
            else
            {
                // Intermediate, "All Levels", "All", or unclassified
                intermediateBucket.Add(ev);
            }
        }

        intermediateBucket = intermediateBucket.OrderByDescending(e => e.SimilarityScore).ToList();
        beginnerBucket = beginnerBucket.OrderByDescending(e => e.SimilarityScore).ToList();
        advancedBucket = advancedBucket.OrderByDescending(e => e.SimilarityScore).ToList();

        int targetIntermediate = (int)Math.Round(targetSize * LevelDistributionRatios.IntermediateRatio);
        int targetBeginner = (int)Math.Round(targetSize * LevelDistributionRatios.BeginnerRatio);
        int targetAdvanced = targetSize - targetIntermediate - targetBeginner;

        var selected = new List<RecommendedEventDto>();
        var selectedIds = new HashSet<string>();

        void AddFromBucket(List<RecommendedEventDto> bucket, int quota)
        {
            int added = 0;
            foreach (var ev in bucket)
            {
                if (added >= quota) break;
                if (selectedIds.Add(ev.Id))
                {
                    selected.Add(ev);
                    added++;
                }
            }
        }

        AddFromBucket(intermediateBucket, targetIntermediate);
        AddFromBucket(beginnerBucket, targetBeginner);
        AddFromBucket(advancedBucket, targetAdvanced);

        // Backfill if any bucket ran short of its quota
        if (selected.Count < targetSize)
        {
            var remaining = candidates
                .Where(e => !selectedIds.Contains(e.Id))
                .OrderByDescending(e => e.SimilarityScore);

            foreach (var ev in remaining)
            {
                if (selected.Count >= targetSize) break;
                selected.Add(ev);
                selectedIds.Add(ev.Id);
            }
        }

        return selected.OrderByDescending(e => e.SimilarityScore).ToList();
    }
}
