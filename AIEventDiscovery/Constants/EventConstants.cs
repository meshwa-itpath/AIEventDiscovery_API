namespace AIEventDiscovery.Constants;

public static class EventRoles
{
    public const string Student = "Student";
    public const string Developer = "Developer";
    public const string DevOpsEngineer = "DevOps Engineer";
    public const string Designer = "Designer";
    public const string ProductManager = "Product Manager";
    public const string ProjectManager = "Project Manager";

    /// <summary>
    /// Checks whether a given role matches the Student role (case-insensitive, handles common variations like "intern").
    /// </summary>
    public static bool IsStudent(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        var normalized = role.Trim().ToLowerInvariant();
        return normalized.Contains("student") || normalized.Contains("intern");
    }
}

public static class EventLevels
{
    public const string All = "All";
    public const string AllLevels = "All Levels";
    public const string Beginner = "Beginner";
    public const string Intermediate = "Intermediate";
    public const string Advanced = "Advanced";
}

public static class LevelDistributionRatios
{
    // Proportional level distribution for professional roles when no explicit level filter is selected
    public const double IntermediateRatio = 0.70; // 70%
    public const double BeginnerRatio = 0.15;     // 15%
    public const double AdvancedRatio = 0.15;     // 15%
}

/// <summary>
/// Classifies user-selectable technologies into two groups used by the recommendation pipeline:
/// <list type="bullet">
///   <item><term>PrimaryEcosystems</term><description>Independent programming stacks. Multiple selections get balanced (equal-quota) result clusters.</description></item>
///   <item><term>CrossCuttingModifiers</term><description>Topic modifiers that are anchored to a primary ecosystem in the query rather than issued as standalone queries.</description></item>
/// </list>
/// Keep this list in sync with the technology options shown in the onboarding / update-profile UI.
/// </summary>
public static class TechnologyTaxonomy
{
    public static readonly IReadOnlySet<string> PrimaryEcosystems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "DotNet Development",
        "Java Development",
        "Python Development",
        "AI & Generative AI",
        "AI Frameworks",
        "Machine Learning",
        "Data Engineering",
        "Mobile Development"
    };

    public static readonly IReadOnlySet<string> CrossCuttingModifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Backend Development",
        "Frontend Development",
        "Database Development",
        "Cloud Computing",
        "DevOps & Infrastructure",
        "Vector Databases",
        "Messaging Systems",
        "CI/CD",
        "Monitoring & Observability",
        "Search Technologies",
        "API Development"
    };

    public static readonly IReadOnlyDictionary<string, string[]> Synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["DotNet Development"] = [".NET", "DotNet", "C#", "ASP.NET", "F#", "Blazor", "Entity Framework", "EF Core"],
        [".NET"] = [".NET", "DotNet", "C#", "ASP.NET", "F#", "Blazor", "Entity Framework", "EF Core"],
        ["Backend Development"] = ["Backend", "Server-Side", "Server Side", "Microservices"],
        ["Frontend Development"] = ["Frontend", "Front-end", "UI", "Web UI", "Client-Side", "React", "Angular", "Vue", "TypeScript", "JavaScript", "Next.js"],
        ["Java Development"] = ["Java", "Spring", "SpringBoot", "Kotlin", "Quarkus"],
        ["Python Development"] = ["Python", "Django", "FastAPI", "Flask"],
        ["AI & Generative AI"] = ["AI", "GenAI", "Generative AI", "LLM", "GPT", "Deep Learning"],
        ["AI Frameworks"] = ["LangChain", "Semantic Kernel", "LlamaIndex", "HuggingFace", "PyTorch", "TensorFlow"],
        ["Machine Learning"] = ["Machine Learning", "ML", "Data Science", "Scikit"],
        ["Data Engineering"] = ["Data Engineering", "Spark", "Kafka", "ETL", "Airflow", "Hadoop"],
        ["Mobile Development"] = ["Mobile", "Android", "iOS", "Flutter", "React Native", "Swift"],
        ["API Development"] = ["API", "REST", "GraphQL", "gRPC", "Web API"],
        ["CI/CD"] = ["CI/CD", "Continuous Integration", "Continuous Deployment", "GitHub Actions", "Jenkins", "GitLab"],
        ["DevOps & Infrastructure"] = ["DevOps", "Docker", "Kubernetes", "K8s", "Terraform", "Ansible", "Helm"],
        ["Database Development"] = ["Database", "SQL", "PostgreSQL", "Postgres", "MySQL", "MongoDB", "Redis"],
        ["Cloud Computing"] = ["Cloud", "AWS", "Azure", "GCP", "Serverless"],
        ["Vector Databases"] = ["Vector", "pgvector", "Pinecone", "Milvus", "Qdrant", "Weaviate"],
        ["Search Technologies"] = ["Search", "Elasticsearch", "OpenSearch", "Lucene", "Solr"],
        ["Monitoring & Observability"] = ["Monitoring", "Observability", "Prometheus", "Grafana", "OpenTelemetry"]
    };

    public static string[] GetSynonyms(string technology) =>
        !string.IsNullOrWhiteSpace(technology) && Synonyms.TryGetValue(technology.Trim(), out var terms)
            ? terms
            : [];
}
