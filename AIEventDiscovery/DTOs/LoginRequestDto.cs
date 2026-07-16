namespace AIEventDiscovery.DTOs;

/// <summary>
/// Request payload for user login.
/// </summary>
public class LoginRequestDto
{
    /// <summary>The registered email address of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The plain-text password to verify against the stored hash.</summary>
    public string Password { get; set; } = string.Empty;
}
