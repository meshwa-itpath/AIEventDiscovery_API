namespace AIEventDiscovery.DTOs;

/// <summary>
/// Response payload returned after a successful login or registration.
/// Contains the JWT token and basic user info so the client can display it immediately.
/// </summary>
public class AuthResponseDto
{
    /// <summary>The signed JWT Bearer token to use in future API requests.</summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>Indicates if the user has completed onboarding.</summary>
    public bool IsOnBoardingCompleted { get; set; }
}
