using System.Text.Json;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Embeddings;

namespace AIEventDiscovery.Services.DataImport;

public class DataImportService : IDataImportService
{
    private readonly string _collectionName;
    private readonly IChromaService _chromaService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(IChromaService chromaService, IEmbeddingService embeddingService, Microsoft.Extensions.Options.IOptions<Configuration.ChromaDbOptions> options, ILogger<DataImportService> logger)
    {
        _chromaService = chromaService;
        _embeddingService = embeddingService;
        _collectionName = options.Value.CollectionName;
        _logger = logger;
    }

    public async Task<(int successCount, string message)> SeedTechnicalEventsAsync(Stream jsonStream)
    {
        // Step 1: Deserialize the uploaded JSON file
        List<TechnicalEventDto>? events;
        try
        {
            events = await JsonSerializer.DeserializeAsync<List<TechnicalEventDto>>(jsonStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse the uploaded JSON file.");
            return (0, "Invalid JSON format. Please check your file structure.");
        }

        if (events == null || events.Count == 0)
            return (0, "The JSON file is empty or contains no events.");

        // Step 2: Ensure the 'technical_events' collection exists — auto-create if missing
        var collectionResponse = await _chromaService.GetCollectionAsync(_collectionName);

        if (!collectionResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Collection '{CollectionName}' not found. Auto-creating...", _collectionName);
            var createResponse = await _chromaService.CreateCollectionAsync(_collectionName);
            if (!createResponse.IsSuccessStatusCode)
            {
                // Return the REAL ChromaDB error so it's visible
                var err = await createResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create collection '{CollectionName}': {Error}", _collectionName, err);
                return (0, $"Failed to create collection '{_collectionName}'. ChromaDB error: {err}");
            }
        }

        // Step 3: Fetch the collection ID (ChromaDB requires ID, not name, for document operations)
        var collectionDetailResponse = await _chromaService.GetCollectionAsync(_collectionName);
        var collectionJson = await collectionDetailResponse.Content.ReadAsStringAsync();
        using var collectionDoc = JsonDocument.Parse(collectionJson);
        var collectionId = collectionDoc.RootElement.GetProperty("id").GetString()!;

        // Step 4: Build ChromaDB add payload
        // Step 4 & 5: Build ChromaDB add payload and send in batches to avoid 'SocketException' / Max Payload limits
        const int batchSize = 100;
        
        for (int i = 0; i < events.Count; i += batchSize)
        {
            var batch = events.Skip(i).Take(batchSize).ToList();
            
            var ids = new List<string>();
            var documents = new List<string>();
            var embeddings = new List<float[]>();
            var metadatas = new List<Dictionary<string, string>>();

            foreach (var ev in batch)
            {
                ids.Add(ev.Id);
                
                var textToEmbed = $"{ev.Title}. {ev.Description}";
                documents.Add(textToEmbed);
                embeddings.Add(_embeddingService.GenerateEmbedding(textToEmbed));

                metadatas.Add(new Dictionary<string, string>
                {
                    ["title"]        = ev.Title,
                    ["category"]     = ev.Category ?? string.Empty,
                    ["subCategory"]  = ev.SubCategory ?? string.Empty,
                    ["technologies"] = string.Join(", ", ev.Technologies),
                    ["tags"]         = string.Join(", ", ev.Tags),
                    ["organizer"]    = ev.Organizer ?? string.Empty,
                    ["city"]         = ev.City ?? string.Empty,
                    ["country"]      = ev.Country ?? string.Empty,
                    ["venue"]        = ev.Venue ?? string.Empty,
                    ["mode"]         = ev.Mode ?? string.Empty,
                    ["level"]        = ev.Level ?? string.Empty,
                    ["eventType"]    = ev.EventType ?? string.Empty,
                    ["startDate"]    = ev.StartDate?.ToString("o") ?? string.Empty,
                    ["endDate"]      = ev.EndDate?.ToString("o") ?? string.Empty,
                    ["rating"]       = ev.Rating?.ToString() ?? string.Empty
                });
            }

            var payload = new { ids, documents, embeddings, metadatas };
            var addResponse = await _chromaService.AddDocumentsAsync(collectionId, payload);
            
            if (!addResponse.IsSuccessStatusCode)
            {
                var error = await addResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add batch starting at index {Index} to collection '{CollectionName}': {Error}", i, _collectionName, error);
                return (0, $"Failed to seed documents at batch {i}: {error}");
            }
        }

        _logger.LogInformation(
            "Successfully seeded {Count} events into collection '{CollectionName}'.",
            events.Count, _collectionName);

        return (events.Count, $"Successfully seeded {events.Count} event(s) into '{_collectionName}'.");
    }
}
