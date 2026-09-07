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
    public async Task<IActionResult> SeedEvents(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Please upload a valid JSON file."));

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Only .json files are accepted."));

        await using var stream = file.OpenReadStream();
        var (successCount, message) = await _dataImportService.SeedTechnicalEventsAsync(stream);

        if (successCount == 0)
            return BadRequest(ApiResponse<object>.Fail(message));

        return Ok(ApiResponse<object>.Ok(new { successCount }, message));
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
