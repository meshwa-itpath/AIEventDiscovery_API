using AIEventDiscovery.DTOs;

namespace AIEventDiscovery.Services.Interfaces;

public interface IRetrievalService
{
    Task<List<RecommendedEventDto>> ExecutePipelineAsync(RetrievalRequest request);
}
