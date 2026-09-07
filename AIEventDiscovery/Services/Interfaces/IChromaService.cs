namespace AIEventDiscovery.Services;

public interface IChromaService
{
    Task<string> GetVersionAsync();
    Task<HttpResponseMessage> GetAllCollectionsAsync();
    Task<HttpResponseMessage> GetCollectionAsync(string collectionName);
    Task<HttpResponseMessage> CreateCollectionAsync(string collectionName, object? metadata = null);
    Task<HttpResponseMessage> AddDocumentsAsync(string collectionId, object payload);
    Task<HttpResponseMessage> UpsertDocumentsAsync(string collectionId, object payload);
    Task<HttpResponseMessage> QueryAsync(string collectionId, object queryPayload);
    Task<HttpResponseMessage> DeleteCollectionAsync(string collectionName);
}
