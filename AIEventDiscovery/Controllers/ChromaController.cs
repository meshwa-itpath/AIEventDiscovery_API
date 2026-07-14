using Microsoft.AspNetCore.Mvc;
using AIEventDiscovery.Services;

namespace AIEventDiscovery.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChromaController : ControllerBase
    {
        private readonly IChromaService _chromaService;

        public ChromaController(IChromaService chromaService)
        {
            _chromaService = chromaService;
        }

        [HttpGet("collections")]
        public async Task<IActionResult> GetAllCollections()
        {
            var response = await _chromaService.GetAllCollectionsAsync();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}
