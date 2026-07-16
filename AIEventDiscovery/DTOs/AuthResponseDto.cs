namespace AIEventDiscovery.DTOs;

/// <summary>
/// Response payload returned after a successful login or registration.
/// Contains the JWT token and basic user info so the client can display it immediately.
/// </summary>
public class AuthResponseDto
{
    /// <summary>The signed JWT Bearer token to use in future API requests.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>The unique ID of the authenticated user.</summary>
    public Guid UserId { get; set; }

    /// <summary>The user's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The user's last name.</summary>
    public string LastName { get; set; } = string.Empty;
}
