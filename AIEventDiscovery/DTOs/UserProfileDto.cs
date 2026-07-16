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
}
