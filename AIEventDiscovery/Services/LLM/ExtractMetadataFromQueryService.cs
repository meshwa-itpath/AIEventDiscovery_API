using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Interfaces;

namespace AIEventDiscovery.Services.LLM;

public class ExtractMetadataFromQueryService : IExtractMetadataFromQueryService
{
    private readonly IGeminiService _geminiService;
    private readonly IGroqService _groqService;
    private readonly ILogger<ExtractMetadataFromQueryService> _logger;

    public ExtractMetadataFromQueryService(
        IGeminiService geminiService, 
        IGroqService groqService,
        ILogger<ExtractMetadataFromQueryService> logger)
    {
        _geminiService = geminiService;
        _groqService = groqService;
        _logger = logger;
    }

    public async Task<QueryUnderstandingResult> ExtractMetadataFromUserQuery(string userQuery, CancellationToken cancellationToken = default)
    {
        // 1. Try Gemini First
        var result = await _geminiService.ExtractMetadataFromUserQuery(userQuery, cancellationToken);
        
        // 2. Check if Gemini succeeded
        if (!result.IsSuccess) 
        {
            _logger.LogWarning("Gemini service failed or was unavailable. Falling back to Groq for query: {Query}", userQuery);
            
            // 3. Fallback to Groq
            var groqResult = await _groqService.ExtractMetadataFromUserQuery(userQuery, cancellationToken);
            return groqResult;
        }

        return result;
    }
}
