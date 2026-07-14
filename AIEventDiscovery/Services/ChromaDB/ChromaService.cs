using System.Net.Http.Json;
using AIEventDiscovery.Configuration;
using Microsoft.Extensions.Options;

namespace AIEventDiscovery.Services;

public class ChromaService : IChromaService
{
    private readonly HttpClient _httpClient;
    private readonly ChromaDbOptions _options;
    private readonly ILogger<ChromaService> _logger;

    public ChromaService(HttpClient httpClient, IOptions<ChromaDbOptions> options, ILogger<ChromaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }
    }

    public async Task<string> GetVersionAsync()
    {
        var response = await _httpClient.GetAsync("/api/v2/version");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<HttpResponseMessage> GetAllCollectionsAsync()
    {
        return await _httpClient.GetAsync("/api/v2/tenants/default_tenant/databases/default_database/collections");
    }

    public async Task<HttpResponseMessage> GetCollectionAsync(string collectionName)
    {
        return await _httpClient.GetAsync($"/api/v2/tenants/default_tenant/databases/default_database/collections/{collectionName}");
    }

    public async Task<HttpResponseMessage> CreateCollectionAsync(string collectionName, object? metadata = null)
    {
        // ChromaDB v2 rejects empty metadata — only send it when explicitly provided
        if (metadata != null)
        {
            var payloadWithMeta = new { name = collectionName, metadata };
            return await _httpClient.PostAsJsonAsync("/api/v2/tenants/default_tenant/databases/default_database/collections", payloadWithMeta);
        }

        var payload = new { name = collectionName };
        return await _httpClient.PostAsJsonAsync("/api/v2/tenants/default_tenant/databases/default_database/collections", payload);
    }

    public async Task<HttpResponseMessage> AddDocumentsAsync(string collectionId, object payload)
    {
        return await _httpClient.PostAsJsonAsync($"/api/v2/tenants/default_tenant/databases/default_database/collections/{collectionId}/add", payload);
    }

    public async Task<HttpResponseMessage> UpsertDocumentsAsync(string collectionId, object payload)
    {
        return await _httpClient.PostAsJsonAsync($"/api/v2/tenants/default_tenant/databases/default_database/collections/{collectionId}/upsert", payload);
    }

    public async Task<HttpResponseMessage> QueryAsync(string collectionId, object queryPayload)
    {
        return await _httpClient.PostAsJsonAsync($"/api/v2/tenants/default_tenant/databases/default_database/collections/{collectionId}/query", queryPayload);
    }
}

