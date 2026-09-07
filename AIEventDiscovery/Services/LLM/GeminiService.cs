using System.Text;
using System.Text.Json;
using AIEventDiscovery.Configuration;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;
using AIEventDiscovery.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AIEventDiscovery.Services.LLM;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> GenerateBatchExplanationsAsync(User user, List<RecommendedEventDto> events)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Gemini API Key is missing. Skipping explanation generation.");
            return new Dictionary<string, string>();
        }

        if (events == null || !events.Any())
        {
            return new Dictionary<string, string>();
        }

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"You are an AI assistant helping a user discover events. The user's role is '{user.Role}' and they are interested in '{user.Technology}'.");
        promptBuilder.AppendLine("Below is a list of recommended events. For each event, explain specifically why it is a good match for this user based on their role and technologies.");
        promptBuilder.AppendLine("Return the response STRICTLY as a JSON object where the keys are the Event IDs and the values are the explanations. Do not include markdown formatting like ```json or any other text.");
        promptBuilder.AppendLine("Events:");
        foreach (var ev in events)
        {
            promptBuilder.AppendLine($"- ID: {ev.Id}, Title: {ev.Title}, Technologies: {string.Join(", ", ev.Technologies)}");
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptBuilder.ToString() }
                    }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        try
        {
            var response = await _httpClient.PostAsync(url, jsonContent);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var textResponse = parts[0].GetProperty("text").GetString();
                        if (!string.IsNullOrWhiteSpace(textResponse))
                        {
                            try
                            {
                                return JsonSerializer.Deserialize<Dictionary<string, string>>(textResponse) ?? new Dictionary<string, string>();
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Failed to parse Gemini response as JSON Dictionary: {Text}", textResponse);
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogError("Gemini API call failed with status code {StatusCode}: {Reason}", response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling Gemini API.");
        }

        return new Dictionary<string, string>();
    }
}
