namespace AIEventDiscovery.DTOs;

/// <summary>
/// Request payload for updating the current user's profile information.
/// Only non-null fields will be applied — null means "leave unchanged".
/// </summary>
public class UpdateProfileRequestDto
{
    /// <summary>The user's new first name. Null = no change.</summary>
    public string? FirstName { get; set; }

    /// <summary>The user's new last name. Null = no change.</summary>
    public string? LastName { get; set; }

    /// <summary>Step 1: The user's new role / specialization (e.g. "Backend Developer"). Null = no change.</summary>
    public string? Role { get; set; }

    /// <summary>Step 2: Primary working stacks (e.g. [".NET", "Node.js"]). Null = no change.</summary>
    public List<string>? PrimaryStacks { get; set; }

    /// <summary>Step 3: Areas of interest / topics (e.g. ["Cloud Computing", "DevOps & Infrastructure"]). Null = no change.</summary>
    public List<string>? Interests { get; set; }
}
