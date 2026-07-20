using System.Text.Json;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services;
using AIEventDiscovery.Services.Embeddings;
using Microsoft.AspNetCore.Mvc;

namespace AIEventDiscovery.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IChromaService _chromaService;
        private readonly IEmbeddingService _embeddingService;
        private readonly string _collectionName;

        public SearchController(IChromaService chromaService, IEmbeddingService embeddingService, Microsoft.Extensions.Options.IOptions<Configuration.ChromaDbOptions> options)
        {
            _chromaService = chromaService;
            _embeddingService = embeddingService;
            _collectionName = options.Value.CollectionName;
        }

        /// <summary>
        /// Performs a semantic vector search on the 'technical_events' collection.
        /// </summary>
        /// <param name="q">The natural-language search query.</param>
        /// <param name="limit">Number of results to return (default: 5).</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(string q, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(ApiResponse<object>.Fail("Query parameter 'q' is required."));

            var queryEmbedding = _embeddingService.GenerateEmbedding(q);

            var collectionResponse = await _chromaService.GetCollectionAsync(_collectionName);
            if (!collectionResponse.IsSuccessStatusCode)
                return NotFound(ApiResponse<object>.Fail($"Collection '{_collectionName}' not found."));

            var collectionJson = await collectionResponse.Content.ReadAsStringAsync();
            using var collectionDoc = JsonDocument.Parse(collectionJson);
            var collectionId = collectionDoc.RootElement.GetProperty("id").GetString()!;

            var queryPayload = new
            {
                query_embeddings = new[] { queryEmbedding },
                n_results = limit
            };

            var resultResponse = await _chromaService.QueryAsync(collectionId, queryPayload);
            if (!resultResponse.IsSuccessStatusCode)
            {
                var error = await resultResponse.Content.ReadAsStringAsync();
                return StatusCode(500, ApiResponse<object>.Fail($"Failed to query ChromaDB: {error}"));
            }

            var resultJson = await resultResponse.Content.ReadAsStringAsync();
            var resultData = JsonSerializer.Deserialize<object>(resultJson);

            return Ok(ApiResponse<object>.Ok(resultData, $"Found results for '{q}'."));
        }
    }
}
