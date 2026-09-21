using System.Collections.Generic;
using System.Threading.Tasks;
using AIEventDiscovery.DTOs;

namespace AIEventDiscovery.Services.Interfaces;

public interface IRecommendationService
{
    Task<ApiResponse<List<RecommendedEventDto>>> GetRecommendedEventsAsync(
        int page,
        int pageSize,
        string? level = null,
        string? mode = null);

    Task<ApiResponse<List<RecommendedEventDto>>> SearchEventsAsync(
        string query,
        int page,
        int pageSize,
        string? level = null,
        string? mode = null);

    Task<ApiResponse<EventDetailDto>> GetEventByIdAsync(Guid id);

    Task<ApiResponse<List<RecommendedEventDto>>> GetRelatedEventsAsync(Guid eventId);
}
