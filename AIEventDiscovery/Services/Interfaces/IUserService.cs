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
}
