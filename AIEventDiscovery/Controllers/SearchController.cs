using Microsoft.AspNetCore.Mvc;
using AIEventDiscovery.Services;
using AIEventDiscovery.Services.Embeddings;
using System.Text.Json;

namespace AIEventDiscovery.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IChromaService _chromaService;
        private readonly IEmbeddingService _embeddingService;

        public SearchController(IChromaService chromaService, IEmbeddingService embeddingService)
        {
            _chromaService = chromaService;
            _embeddingService = embeddingService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string q, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query parameter 'q' is required.");

            // 1. Get embedding for the query
            var queryEmbedding = _embeddingService.GenerateEmbedding(q);

            // 2. Get the collection ID
            var collectionResponse = await _chromaService.GetCollectionAsync("technical_events");
            if (!collectionResponse.IsSuccessStatusCode)
                return NotFound("Collection 'technical_events' not found.");

            var collectionJson = await collectionResponse.Content.ReadAsStringAsync();
            using var collectionDoc = JsonDocument.Parse(collectionJson);
            var collectionId = collectionDoc.RootElement.GetProperty("id").GetString()!;

            // 3. Query ChromaDB
            var queryPayload = new
            {
                query_embeddings = new[] { queryEmbedding },
                n_results = limit
            };

            var resultResponse = await _chromaService.QueryAsync(collectionId, queryPayload);
            if (!resultResponse.IsSuccessStatusCode)
            {
                var error = await resultResponse.Content.ReadAsStringAsync();
                return StatusCode(500, $"Failed to query ChromaDB: {error}");
            }

            var resultJson = await resultResponse.Content.ReadAsStringAsync();

            return Content(resultJson, "application/json");
        }
    }
}
