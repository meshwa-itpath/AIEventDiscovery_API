using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;

namespace AIEventDiscovery.Services.Interfaces;

public interface IGeminiService
{
    Task<Dictionary<string, string>> GenerateBatchExplanationsAsync(User user, List<RecommendedEventDto> events);
}
