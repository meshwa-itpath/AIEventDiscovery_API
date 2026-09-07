namespace AIEventDiscovery.Services.DataImport;

public interface IDataImportService
{
    /// <summary>
    /// Seeds technical events from a JSON stream into the 'technical_events' ChromaDB collection.
    /// Events are appended — multiple file uploads accumulate into the same collection without overriding.
    /// </summary>
    Task<(int successCount, string message)> SeedTechnicalEventsAsync(Stream jsonStream);

    /// <summary>
    /// Clears/deletes the 'technical_events' ChromaDB collection and all its stored vectors.
    /// </summary>
    Task<(bool success, string message)> ClearCollectionAsync();
}
