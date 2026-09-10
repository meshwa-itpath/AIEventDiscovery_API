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
