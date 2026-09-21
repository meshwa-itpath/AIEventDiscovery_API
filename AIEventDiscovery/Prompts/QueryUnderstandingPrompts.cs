namespace AIEventDiscovery.Prompts;

public static class QueryUnderstandingPrompts
{
    public static string BuildPrompt(string userQuery)
    {
        return $$"""
          You are an AI specialized in query understanding for a technical event search engine.
          Analyze the following user search query and extract two components:

          1. "mainQuery": A clean, concise semantic sentence/phrase representing ONLY the core technical topic or domain intent.
            - Strip out locations (e.g., India, Bangalore), modes (e.g., Online, In-Person), attendee levels (e.g., Beginner, Advanced), formats (e.g., Workshop, Conference), and preference words (e.g., preferably, ideally).
            - Example: "Find advanced .NET backend events in India, preferably online." -> "mainQuery": ".NET backend development"

          2. "metadata": An array of extracted filter objects with properties:
            - "field": Must be one of: Technology, Country, City, Venue, Mode, Level, EventType, Category, SubCategory, Organizer, Tags.
            - "values": An array of matching values (e.g., [".NET"], ["India"], ["Online"]).
            - "type":
              - "Hard": Mandatory / strict constraint explicitly required by the user (e.g., "in India", ".NET", "Advanced level", "must be", "only").
              - "Soft": Optional preference or nice-to-have indicated by the user (e.g., "preferably online", "ideally", "if possible", "prefer").

          Respond ONLY with a valid JSON object matching this schema without markdown fences or additional text:
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

          User Query: "{{userQuery}}"
          """;
    }
}
