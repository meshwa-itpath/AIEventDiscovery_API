using AIEventDiscovery.DTOs;

namespace AIEventDiscovery.Services;

/// <summary>
/// Defines the contract for authentication-related operations:
/// user registration, login, and profile retrieval.
/// The service returns a ready-to-send ApiResponse — the controller just passes it through.
/// </summary>
public interface IAuthService
{
    /// <summary>Registers a new user. Returns Fail response if email is already taken.</summary>
    Task<ApiResponse<bool>> RegisterAsync(RegisterRequestDto request);

    /// <summary>Validates credentials. Returns Fail response if email/password is wrong.</summary>
    Task<ApiResponse<string>> LoginAsync(LoginRequestDto request);

    /// <summary>Retrieves the public profile for the given user. Returns Fail response if not found.</summary>
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId);
}
