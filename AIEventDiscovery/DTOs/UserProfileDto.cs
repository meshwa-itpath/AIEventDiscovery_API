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

    /// <summary>The user's role (e.g. "Backend Developer").</summary>
    public string? Role { get; set; }

    /// <summary>List of technologies the user works with.</summary>
    public List<string>? Technology { get; set; }
}
