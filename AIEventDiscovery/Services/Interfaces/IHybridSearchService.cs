using AIEventDiscovery.DTOs;

namespace AIEventDiscovery.Services.Interfaces;

public interface IHybridSearchService
{
    Task<ApiResponse<List<RecommendedEventDto>>> SearchEventsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
}
