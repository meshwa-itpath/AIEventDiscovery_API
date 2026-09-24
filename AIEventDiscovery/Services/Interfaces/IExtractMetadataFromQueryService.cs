using AIEventDiscovery.DTOs;

namespace AIEventDiscovery.Services.Interfaces;

public interface IExtractMetadataFromQueryService
{
    Task<QueryUnderstandingResult> ExtractMetadataFromUserQuery(string userQuery, CancellationToken cancellationToken = default);
}
