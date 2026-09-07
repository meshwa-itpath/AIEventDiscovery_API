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

    /// <summary>The user's new role (e.g. "Backend Developer"). Null = no change.</summary>
    public string? Role { get; set; }

    /// <summary>List of technologies. Null = no change. Empty list = clear technologies.</summary>
    public List<string>? Technology { get; set; }
}
