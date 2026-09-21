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
    private readonly IGenericRepository<Event> _eventRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRetrievalService _retrievalService;

    public RecommendationService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Event> eventRepository,
        ICurrentUserService currentUserService,
        IRetrievalService retrievalService)
    {
        _userRepository = userRepository;
        _eventRepository = eventRepository;
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
            level = null;

        if (string.Equals(mode, "All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(mode))
            mode = null;

        var (primaries, modifiers, allTechnologies) = RecommendationQueryBuilder.ResolveUserPreferences(user);

        // Determine effective level filter vs proportional distribution
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

        // Pool size = pageSize × 5, capped at 50
        int poolSize = Math.Min(pageSize * 5, 50);

        List<RecommendedEventDto> fullRankedPool;

        if (primaries.Count == 0 && modifiers.Count == 0)
        {
            // No tech interests on profile — run a single generic query
            fullRankedPool = await FetchGenericPoolAsync(user, queryFilters, poolSize);
        }
        else
        {
            fullRankedPool = await FetchClusteredPoolAsync(user, primaries, modifiers, allTechnologies, queryFilters, poolSize);
        }

        // Apply proportional level distribution for professional roles with no explicit level filter
        if (applyProportionalBlending)
        {
            fullRankedPool = ApplyProportionalLevelDistribution(fullRankedPool, poolSize);
        }

        if (fullRankedPool.Count == 0)
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok([], "No matching events found.");
        }

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
            level = null;

        if (string.Equals(mode, "All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(mode))
            mode = null;

        List<string>? searchLevels = !string.IsNullOrWhiteSpace(level) ? [level] : null;
        var queryFilters = RecommendationQueryBuilder.BuildQueryFilters(searchLevels, mode);

        int poolSize = Math.Min(pageSize * 5, 50);

        var request = new RetrievalRequest
        {
            QueryText = query,
            QueryFilters = queryFilters,
            Limit = poolSize,
            FinalLimit = poolSize,
            SimilarityThreshold = 0.6,
            EnableReRanking = true,
            FilterExpiredEvents = true,
            EnableGeminiExplanation = false,
            UserContext = user
        };

        var fullRankedPool = await _retrievalService.ExecutePipelineAsync(request);

        if (fullRankedPool.Count == 0)
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok([], "No matching events found.");
        }

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

    public async Task<ApiResponse<EventDetailDto>> GetEventByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return ApiResponse<EventDetailDto>.Fail("Invalid event ID.");
        }

        var eventDetail = await _eventRepository.GetByIdAsync(id, e => new EventDetailDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Category = e.Category,
            SubCategory = e.SubCategory,
            Technologies = e.Technologies,
            Tags = e.Tags,
            Organizer = e.Organizer,
            City = e.City,
            Country = e.Country,
            Venue = e.Venue,
            Mode = e.Mode,
            Level = e.Level,
            EventType = e.EventType,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Rating = e.Rating,
            CreatedAt = e.CreatedAt
        });

        if (eventDetail == null)
        {
            return ApiResponse<EventDetailDto>.Fail("Event not found.");
        }

        return ApiResponse<EventDetailDto>.Ok(eventDetail, "Event details retrieved successfully.");
    }

    public async Task<ApiResponse<List<RecommendedEventDto>>> GetRelatedEventsAsync(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            return ApiResponse<List<RecommendedEventDto>>.Fail("Invalid event ID.");
        }

        var targetEvent = await _eventRepository.GetByIdAsync<Event>(eventId);
        if (targetEvent == null)
        {
            return ApiResponse<List<RecommendedEventDto>>.Fail("Target event not found.");
        }

        var terms = new List<string> { targetEvent.Title };
        if (targetEvent.Technologies != null && targetEvent.Technologies.Any())
        {
            terms.AddRange(targetEvent.Technologies);
        }
        if (targetEvent.Tags != null && targetEvent.Tags.Any())
        {
            terms.AddRange(targetEvent.Tags);
        }

        var queryText = string.Join(" ", terms);

        // Optional: fetch user context if we want personalized explanations or ranking
        User? userContext = null;
        var userId = _currentUserService.UserId;
        if (userId != Guid.Empty)
        {
            userContext = await _userRepository.GetByIdAsync<User>(userId);
        }

        int finalLimit = 10;
        int fetchLimit = finalLimit + 5; // Fetch a bit more to ensure we have enough after excluding target

        var request = new RetrievalRequest
        {
            QueryText = queryText,
            QueryFilters = null, // No strict metadata filters based on user feedback
            Limit = fetchLimit,
            FinalLimit = fetchLimit,
            SimilarityThreshold = 0.6,
            EnableReRanking = true,
            FilterExpiredEvents = true,
            EnableGeminiExplanation = false,
            UserContext = userContext
        };

        var relatedEvents = await _retrievalService.ExecutePipelineAsync(request);

        // Exclude the target event itself
        var filteredEvents = relatedEvents
            .Where(e => !string.Equals(e.Id, eventId.ToString(), StringComparison.OrdinalIgnoreCase))
            .Take(finalLimit)
            .ToList();

        if (filteredEvents.Count == 0)
        {
            return ApiResponse<List<RecommendedEventDto>>.Ok([], "No related events found.");
        }

        return ApiResponse<List<RecommendedEventDto>>.Ok(filteredEvents, "Successfully retrieved related events.");
    }

    // ── Private Pipeline Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Fetches a candidate pool using a single generic query when the user has no technology interests.
    /// </summary>
    private async Task<List<RecommendedEventDto>> FetchGenericPoolAsync(
        User user,
        EventQueryFilters? queryFilters,
        int poolSize)
    {
        var request = new RetrievalRequest
        {
            QueryText = RecommendationQueryBuilder.BuildGenericQuery(user),
            QueryFilters = queryFilters,
            Limit = poolSize,
            FinalLimit = poolSize,
            SimilarityThreshold = 0.7,
            EnableReRanking = true,
            FilterExpiredEvents = true,
            EnableGeminiExplanation = false,
            UserContext = user
        };

        return await _retrievalService.ExecutePipelineAsync(request);
    }

    /// <summary>
    /// Fetches candidates per primary ecosystem cluster, distributes quota equally across clusters,
    /// applies multi-tag overlap boosting, and returns a deduplicated ranked pool.
    /// </summary>
    private async Task<List<RecommendedEventDto>> FetchClusteredPoolAsync(
        User user,
        List<string> primaries,
        List<string> modifiers,
        List<string> allUserTechnologies,
        EventQueryFilters? queryFilters,
        int poolSize)
    {
        // Each primary ecosystem cluster gets an equal share of the total pool
        int quotaPerCluster = (int)Math.Ceiling((double)poolSize / primaries.Count);
        // Fetch slightly more per cluster to account for deduplication loss
        int limitPerCluster = Math.Max(15, (int)(quotaPerCluster * 1.5));

        // Collect candidates per cluster maintaining cluster identity for round-robin
        var clusterBuckets = new Dictionary<string, List<RecommendedEventDto>>(StringComparer.OrdinalIgnoreCase);

        foreach (var primary in primaries)
        {
            var queryText = RecommendationQueryBuilder.BuildAnchoredQuery(user, primary, modifiers);
            var request = new RetrievalRequest
            {
                QueryText = queryText,
                QueryFilters = queryFilters,
                Limit = limitPerCluster,
                FinalLimit = limitPerCluster,
                SimilarityThreshold = 0.75,
                EnableReRanking = true,
                FilterExpiredEvents = true,
                EnableGeminiExplanation = false,
                UserContext = user
            };

            var clusterResults = await _retrievalService.ExecutePipelineAsync(request);

            // Apply weighted primary/modifier ranking
            ApplyWeightedRanking(clusterResults, primary, modifiers);

            // Re-sort within the cluster after ranking
            //clusterResults.Sort((a, b) => b.RankingScore.CompareTo(a.RankingScore));
            clusterBuckets[primary] = clusterResults;
        }

        // Deduplicate across clusters: keep the entry with the highest score per event
        var deduplicated = new Dictionary<string, RecommendedEventDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, events) in clusterBuckets)
        {
            foreach (var ev in events)
            {
                if (!deduplicated.TryGetValue(ev.Id, out var existing) || ev.RankingScore > existing.RankingScore)
                {
                    deduplicated[ev.Id] = ev;
                }
            }
        }

        // Re-group deduplicated events back into cluster buckets (by best-cluster assignment)
        var regrouped = new Dictionary<string, List<RecommendedEventDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var primary in primaries)
        {
            regrouped[primary] = [];
        }

        foreach (var ev in deduplicated.Values)
        {
            // Assign event to the cluster whose query best matched it (highest score contribution)
            // Simple heuristic: assign to the cluster bucket it originally appeared in first
            bool assigned = false;
            foreach (var primary in primaries)
            {
                if (clusterBuckets[primary].Any(e => e.Id == ev.Id))
                {
                    regrouped[primary].Add(ev);
                    assigned = true;
                    break;
                }
            }
            // Safety fallback: assign to first cluster if not matched
            if (!assigned)
            {
                regrouped[primaries[0]].Add(ev);
            }
        }

        // Ensure each cluster bucket is sorted by score descending
        foreach (var key in regrouped.Keys.ToList())
        {
            regrouped[key].Sort((a, b) => b.RankingScore.CompareTo(a.RankingScore));
        }

        // Round-robin across clusters to guarantee balanced representation
        var roundRobinPool = new List<RecommendedEventDto>(poolSize);
        var pointers = primaries.ToDictionary(p => p, _ => 0, StringComparer.OrdinalIgnoreCase);
        bool addedAny = true;

        while (addedAny && roundRobinPool.Count < poolSize)
        {
            addedAny = false;
            foreach (var primary in primaries)
            {
                if (roundRobinPool.Count >= poolSize) break;

                int ptr = pointers[primary];
                if (ptr < regrouped[primary].Count && ptr < quotaPerCluster)
                {
                    roundRobinPool.Add(regrouped[primary][ptr]);
                    pointers[primary]++;
                    addedAny = true;
                }
            }
        }

        // Final global sort by ranking score — highest quality events rise to the top
        //roundRobinPool.Sort((a, b) => b.RankingScore.CompareTo(a.RankingScore));
        return roundRobinPool;
    }

    /// <summary>
    /// Computes a weighted RankingScore for each event based on user preferences:
    ///   - Primary technology match receives the strongest boost (+0.30)
    ///   - Modifiers provide smaller boosts (+0.05 per modifier, up to +0.10)
    ///   - Primary + modifier combined match receives an additional synergy boost (+0.15)
    ///   - Modifier-only events receive a smaller boost (+0.03, capped at +0.05) and cannot
    ///     outrank primary-focused events regardless of raw vector similarity.
    /// Preserves the raw pgvector SimilarityScore untouched.
    /// </summary>
    internal static void ApplyWeightedRanking(
        List<RecommendedEventDto> events,
        string primaryTech,
        List<string> modifiers)
    {
        const double PrimaryBoost = 0.30;
        const double ModifierBoost = 0.05;
        const double SynergyBoost = 0.15;
        const double MaxModifierBoost = 0.10;
        const double ModifierOnlyCap = 0.05;

        foreach (var ev in events)
        {
            double score = ev.SimilarityScore;
            bool hasPrimaryMatch = MatchesTechnology(ev, primaryTech);

            int matchedModifiersCount = 0;
            foreach (var mod in modifiers)
            {
                if (MatchesTechnology(ev, mod))
                {
                    matchedModifiersCount++;
                }
            }

            if (hasPrimaryMatch)
            {
                score += PrimaryBoost;
                double modBoost = Math.Min(matchedModifiersCount * ModifierBoost, MaxModifierBoost);
                score += modBoost;

                if (matchedModifiersCount > 0)
                {
                    score += SynergyBoost;
                }
            }
            else if (matchedModifiersCount > 0)
            {
                // Modifier-only match: keep small so primary-technology events dominate
                score += Math.Min(matchedModifiersCount * 0.03, ModifierOnlyCap);
            }

            ev.RankingScore = Math.Round(score, 4);
        }
    }

    private static bool MatchesTechnology(RecommendedEventDto ev, string technology)
    {
        if (string.IsNullOrWhiteSpace(technology)) return false;

        var tokens = TechnologyTaxonomy.Synonyms.TryGetValue(technology.Trim(), out var synonyms)
            ? synonyms
            : [technology.Trim()];

        // 1. Check Technologies list
        if (ev.Technologies != null)
        {
            foreach (var t in ev.Technologies)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                foreach (var token in tokens)
                {
                    if (ItemMatchesToken(t, token))
                        return true;
                }
            }
        }

        // 2. Check Tags list
        if (ev.Tags != null)
        {
            foreach (var tag in ev.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                foreach (var token in tokens)
                {
                    if (ItemMatchesToken(tag, token))
                        return true;
                }
            }
        }

        // 3. Check Title fallback
        if (!string.IsNullOrWhiteSpace(ev.Title))
        {
            foreach (var token in tokens)
            {
                if (token.Length >= 2 && ev.Title.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }

    private static bool ItemMatchesToken(string item, string token)
    {
        if (string.Equals(item, token, StringComparison.OrdinalIgnoreCase))
            return true;

        if (token.Length <= 2)
            return false;

        return item.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Distributes candidates proportionally according to LevelDistributionRatios:
    /// ~70% Intermediate (+ All Levels / unclassified), ~15% Beginner, ~15% Advanced.
    /// Any shortfall in a bucket is backfilled from remaining candidates ordered by score.
    /// </summary>
    private static List<RecommendedEventDto> ApplyProportionalLevelDistribution(
        List<RecommendedEventDto> candidates,
        int targetSize)
    {
        if (candidates.Count <= targetSize)
        {
            return candidates.OrderByDescending(e => e.RankingScore).ToList();
        }

        var beginnerBucket   = new List<RecommendedEventDto>();
        var advancedBucket   = new List<RecommendedEventDto>();
        var intermediateBucket = new List<RecommendedEventDto>();

        foreach (var ev in candidates)
        {
            var lvl = ev.Level?.Trim().ToLowerInvariant();
            if (lvl == "beginner")
                beginnerBucket.Add(ev);
            else if (lvl == "advanced")
                advancedBucket.Add(ev);
            else
                intermediateBucket.Add(ev); // Intermediate, "All Levels", "All", or unclassified
        }

        intermediateBucket.Sort((a, b) => b.RankingScore.CompareTo(a.RankingScore));
        beginnerBucket.Sort((a, b)     => b.RankingScore.CompareTo(a.RankingScore));
        advancedBucket.Sort((a, b)     => b.RankingScore.CompareTo(a.RankingScore));

        int targetIntermediate = (int)Math.Round(targetSize * LevelDistributionRatios.IntermediateRatio);
        int targetBeginner     = (int)Math.Round(targetSize * LevelDistributionRatios.BeginnerRatio);
        int targetAdvanced     = targetSize - targetIntermediate - targetBeginner;

        var selected    = new List<RecommendedEventDto>(targetSize);
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

        // Backfill any shortfall from remaining candidates
        if (selected.Count < targetSize)
        {
            foreach (var ev in candidates.OrderByDescending(e => e.RankingScore))
            {
                if (selected.Count >= targetSize) break;
                if (selectedIds.Add(ev.Id))
                {
                    selected.Add(ev);
                }
            }
        }

        return selected.OrderByDescending(e => e.RankingScore).ToList();
    }

    // ── Utilities ────────────────────────────────────────────────────────────────

    private static List<string> ParseTechnologies(string? technology) =>
        technology?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? [];
}
