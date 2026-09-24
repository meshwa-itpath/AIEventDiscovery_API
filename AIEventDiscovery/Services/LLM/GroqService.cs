using System.Text;
using System.Text.Json;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Interfaces;

namespace AIEventDiscovery.Services.LLM;

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqService> _logger;
    private readonly IConfiguration _configuration;

    public GroqService(HttpClient httpClient, ILogger<GroqService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<QueryUnderstandingResult> ExtractMetadataFromUserQuery(string userQuery, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Groq:ApiKey"];
        var model = _configuration["Groq:Model"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Groq API Key is missing. Falling back to original query.");
            return new QueryUnderstandingResult { MainQuery = userQuery, IsSuccess = false };
        }

        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return new QueryUnderstandingResult { IsSuccess = false };
        }

        var prompt = Prompts.QueryUnderstandingPrompts.BuildPrompt(userQuery);

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    {
                        var textResponse = content.GetString();
                        if (!string.IsNullOrWhiteSpace(textResponse))
                        {
                            try
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var result = JsonSerializer.Deserialize<QueryUnderstandingResult>(textResponse, options);
                                if (result != null)
                                {
                                    return result;
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Failed to parse Groq response as QueryUnderstandingResult: {Text}", textResponse);
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogError("Groq API call failed with status code {StatusCode}: {Reason}", response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling Groq API for query understanding.");
        }

        // Fallback
        return new QueryUnderstandingResult { MainQuery = userQuery, IsSuccess = false };
    }
}
