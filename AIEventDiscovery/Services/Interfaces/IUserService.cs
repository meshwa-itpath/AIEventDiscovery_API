using AIEventDiscovery.DTOs;

namespace AIEventDiscovery.Services.Interfaces;

/// <summary>
/// Defines the contract for user-related operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Saves or updates the onboarding details of a user.
    /// </summary>
    Task<ApiResponse<bool>> SaveOnBoardingDetailAsync(OnBoardingDetailRequestDto request);

    /// <summary>
    /// Returns the profile (FirstName, LastName, Role, Technology) of the current user.
    /// </summary>
    Task<ApiResponse<UserProfileDto>> GetProfileAsync();

    /// <summary>
    /// Updates the profile fields (FirstName, LastName, Role, Technology) of the current user.
    /// Only non-null fields in the request are applied.
    /// </summary>
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(UpdateProfileRequestDto request);
}
