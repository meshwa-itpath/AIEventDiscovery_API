using System.Linq;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Embeddings;
using AIEventDiscovery.Services.PgVector;
using Microsoft.AspNetCore.Mvc;

namespace AIEventDiscovery.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IPgVectorService _pgVectorService;
        private readonly IEmbeddingService _embeddingService;

        public SearchController(IPgVectorService pgVectorService, IEmbeddingService embeddingService)
        {
            _pgVectorService = pgVectorService;
            _embeddingService = embeddingService;
        }

        /// <summary>
        /// Performs a semantic vector search on the Events table.
        /// </summary>
        /// <param name="q">The natural-language search query.</param>
        /// <param name="limit">Number of results to return (default: 5).</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(string q, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(ApiResponse<object>.Fail("Query parameter 'q' is required."));

            try
            {
                var queryEmbedding = _embeddingService.GenerateEmbedding(q);

                // Query pgvector for similar events
                var results = await _pgVectorService.QuerySimilarAsync(queryEmbedding, limit);

                // Map results to anonymous objects for the API response
                var mappedResults = results.Select(r => new
                {
                    Id = r.Event.Id,
                    Title = r.Event.Title,
                    Description = r.Event.Description,
                    SimilarityScore = r.SimilarityScore
                }).ToList();

                return Ok(ApiResponse<object>.Ok(mappedResults, $"Found {mappedResults.Count} results for '{q}'."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail($"Failed to execute vector search: {ex.Message}"));
            }
        }
    }
}
