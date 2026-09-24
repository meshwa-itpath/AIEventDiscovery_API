using AIEventDiscovery.DTOs;

namespace AIEventDiscovery.Services.Interfaces;

public interface IGroqService
{
    Task<QueryUnderstandingResult> ExtractMetadataFromUserQuery(string userQuery, CancellationToken cancellationToken = default);
}
