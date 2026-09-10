using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.DataImport;
using Microsoft.AspNetCore.Mvc;

namespace AIEventDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataImportController : ControllerBase
{
    private readonly IDataImportService _dataImportService;

    public DataImportController(IDataImportService dataImportService)
    {
        _dataImportService = dataImportService;
    }

    /// <summary>
    /// Uploads a JSON file of technical events and seeds them into the 'technical_events' ChromaDB collection.
    /// Multiple file uploads are supported — all events are appended without overriding existing data.
    /// </summary>
    [HttpPost("seed-events")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedEvents(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Please upload at least one valid JSON file."));

        int totalSeeded = 0;
        var results = new List<string>();

        foreach (var file in files)
        {
            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                results.Add($"'{file.FileName}' skipped: Only .json files are accepted.");
                continue;
            }

            await using var stream = file.OpenReadStream();
            var (successCount, message) = await _dataImportService.SeedTechnicalEventsAsync(stream);
            
            totalSeeded += successCount;
            results.Add($"'{file.FileName}': {message}");
        }

        return Ok(ApiResponse<object>.Ok(
            new { totalSeeded, details = results }, 
            $"Successfully processed files. Total events seeded: {totalSeeded}"));
    }

    /// <summary>
    /// Deletes the 'technical_events' ChromaDB collection and all its dumped data.
    /// </summary>
    [HttpDelete("clear-collection")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClearCollection()
    {
        var (success, message) = await _dataImportService.ClearCollectionAsync();
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(message));

        return Ok(ApiResponse<object>.Ok(null, message));
    }
}
