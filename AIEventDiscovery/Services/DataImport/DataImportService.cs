using System.Text.Json;
using AIEventDiscovery.Data;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;
using AIEventDiscovery.Services.Embeddings;
using AIEventDiscovery.Services.PgVector;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace AIEventDiscovery.Services.DataImport;

public class DataImportService : IDataImportService
{
    private readonly IPgVectorService _pgVectorService;
    private readonly ApplicationDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(
        IPgVectorService pgVectorService,
        ApplicationDbContext db,
        IEmbeddingService embeddingService,
        ILogger<DataImportService> logger)
    {
        _pgVectorService = pgVectorService;
        _db = db;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<(int successCount, string message)> SeedTechnicalEventsAsync(Stream jsonStream)
    {
        // Step 1: Deserialize the uploaded JSON file
        List<TechnicalEventDto>? events;
        try
        {
            events = await JsonSerializer.DeserializeAsync<List<TechnicalEventDto>>(jsonStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse the uploaded JSON file.");
            return (0, "Invalid JSON format. Please check your file structure.");
        }

        if (events == null || events.Count == 0)
            return (0, "The JSON file is empty or contains no events.");

        // Step 2: Process in batches — generate embeddings and build Event entities
        const int batchSize = 100;
        int totalSeeded = 0;

        for (int i = 0; i < events.Count; i += batchSize)
        {
            var batch = events.Skip(i).Take(batchSize).ToList();
            var eventEntities = new List<Event>();

            foreach (var dto in batch)
            {
                var textToEmbed = $"{dto.Title}. {dto.Description}";
                var embeddingArray = _embeddingService.GenerateEmbedding(textToEmbed);

                eventEntities.Add(new Event
                {
                    Id          = Guid.NewGuid(),
                    Title       = dto.Title,
                    Description = dto.Description,
                    Category    = dto.Category,
                    SubCategory = dto.SubCategory,
                    Technologies = dto.Technologies,
                    Tags        = dto.Tags,
                    Organizer   = dto.Organizer,
                    City        = dto.City,
                    Country     = dto.Country,
                    Venue       = dto.Venue,
                    Mode        = dto.Mode,
                    Level       = dto.Level,
                    EventType   = dto.EventType,
                    StartDate   = dto.StartDate?.ToUniversalTime(),
                    EndDate     = dto.EndDate?.ToUniversalTime(),
                    Rating      = dto.Rating,
                    Embedding   = new Vector(embeddingArray),
                    CreatedAt   = DateTime.UtcNow
                });
            }

            try
            {
                var seeded = await _pgVectorService.UpsertEventsAsync(eventEntities);
                totalSeeded += seeded;
                _logger.LogInformation("Seeded batch {Start}–{End} ({Count} events).", i, i + batch.Count - 1, seeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed batch starting at index {Index}.", i);
                return (totalSeeded, $"Failed at batch starting index {i}: {ex.Message}");
            }
        }

        _logger.LogInformation("Successfully seeded {Count} events into pgvector.", totalSeeded);
        return (totalSeeded, $"Successfully seeded {totalSeeded} event(s) into the Events table.");
    }

    public async Task<(bool success, string message)> ClearCollectionAsync()
    {
        try
        {
            await _pgVectorService.DeleteAllEventsAsync();
            _logger.LogInformation("Successfully cleared all events from the Events table.");
            return (true, "All events deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete events.");
            return (false, $"Failed to delete events: {ex.Message}");
        }
    }
}
