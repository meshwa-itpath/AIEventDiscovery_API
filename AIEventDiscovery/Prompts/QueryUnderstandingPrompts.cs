namespace AIEventDiscovery.Prompts;

public static class QueryUnderstandingPrompts
{
    public static string BuildPrompt(string userQuery)
    {
        return $$"""
          You are an AI specialized in query understanding for a technical event search engine. Extract:

          1. "mainQuery": Concise semantic phrase representing ONLY the core technical topic/domain intent (strip locations, modes, levels, formats, and preference words).
          2. "metadata": Array of filter objects with:
             - "field": One of [Technology, Country, City, Venue, Mode, Level, EventType, Category, SubCategory, Organizer, Tags].
             - "values": Array of matching values.
             - "type": "Hard" (mandatory/strict constraint) or "Soft" (preference/optional).

          Output ONLY a valid JSON object (no markdown, no additional text):
          {
            "mainQuery": "string",
            "metadata": [
              {
                "field": "string",
                "values": ["string"],
                "type": "Hard" | "Soft"
              }
            ]
          }

          Example:
          Input: "Find advanced .NET backend events in India, preferably online."
          Output: {"mainQuery":".NET backend development","metadata":[{"field":"Technology","values":[".NET"],"type":"Hard"},{"field":"Level","values":["Advanced"],"type":"Hard"},{"field":"Country","values":["India"],"type":"Hard"},{"field":"Mode","values":["Online"],"type":"Soft"}]}

          User Query: $"{{userQuery}}"
          """;
    }
}
