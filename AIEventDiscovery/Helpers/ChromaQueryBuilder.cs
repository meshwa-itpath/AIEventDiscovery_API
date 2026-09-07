using System.Collections.Generic;
using System.Linq;

namespace AIEventDiscovery.Helpers;

public static class ChromaQueryBuilder
{
    public static object BuildQueryPayload(
        float[] queryEmbedding,
        List<string> technologies,
        int limit,
        string? level = null,
        string? mode = null)
    {
        // 1. Build where_document filter (for technologies substring matching in text)
        object? whereDocument = null;
        if (technologies != null && technologies.Any())
        {
            if (technologies.Count == 1)
            {
                whereDocument = new Dictionary<string, string>
                {
                    { "$contains", technologies[0] }
                };
            }
            else
            {
                var orConditions = technologies.Select(tech => new Dictionary<string, string>
                {
                    { "$contains", tech }
                }).ToArray();

                whereDocument = new Dictionary<string, object>
                {
                    { "$or", orConditions }
                };
            }
        }

        // 2. Build where filter (for level and mode metadata matching)
        object? where = null;
        var metadataFilters = new List<Dictionary<string, object>>();

        if (!string.IsNullOrWhiteSpace(level))
        {
            metadataFilters.Add(new Dictionary<string, object> { { "level", level } });
        }

        if (!string.IsNullOrWhiteSpace(mode))
        {
            metadataFilters.Add(new Dictionary<string, object> { { "mode", mode } });
        }

        if (metadataFilters.Any())
        {
            if (metadataFilters.Count == 1)
            {
                where = metadataFilters[0];
            }
            else
            {
                where = new Dictionary<string, object>
                {
                    { "$and", metadataFilters.ToArray() }
                };
            }
        }

        // 3. Assemble full payload
        var payload = new Dictionary<string, object>
        {
            { "query_embeddings", new[] { queryEmbedding } },
            { "n_results", limit }
        };

        if (where != null)
        {
            payload.Add("where", where);
        }

        if (whereDocument != null)
        {
            payload.Add("where_document", whereDocument);
        }

        return payload;
    }
}
