namespace AIEventDiscovery.DTOs;

/// <summary>
/// The public profile of the currently authenticated user.
/// Only non-sensitive fields are exposed — no password hash.
/// </summary>
public class UserProfileDto
{
    /// <summary>The user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The user's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Step 1: The user's role / specialization (e.g. "Backend Developer").</summary>
    public string? Role { get; set; }

    /// <summary>Step 2: Primary working stacks (e.g. [".NET", "Node.js"]).</summary>
    public List<string>? PrimaryStacks { get; set; }

    /// <summary>Step 3: Areas of interest / topics (e.g. ["Cloud Computing", "DevOps & Infrastructure"]).</summary>
    public List<string>? Interests { get; set; }
}
