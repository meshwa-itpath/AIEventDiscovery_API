using AIEventDiscovery.Data.Repositories;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;
using AIEventDiscovery.Services.Interfaces;

namespace AIEventDiscovery.Services;

public class UserService : IUserService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UserService(IGenericRepository<User> userRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<bool>> SaveOnBoardingDetailAsync(OnBoardingDetailRequestDto request)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return ApiResponse<bool>.Fail("User not authenticated.");
        }

        var user = await _userRepository.GetByIdAsync<User>(userId);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("User not found.");
        }

        if (!user.IsOnBoardingCompleted)
        {
            user.IsOnBoardingCompleted = true;
        }

        user.Role = request.Role;
        user.Technology = request.Technology == null || !request.Technology.Any()
            ? null
            : string.Join(",", request.Technology);
        user.UpdatedBy = userId;

        await _userRepository.UpsertAsync(user);

        return ApiResponse<bool>.Ok(true, "Onboarding details saved successfully.");
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return ApiResponse<UserProfileDto>.Fail("User not authenticated.");
        }

        var user = await _userRepository.GetByIdAsync<User>(userId);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("User not found.");
        }

        var profileDto = new UserProfileDto
        {
            FirstName  = user.FirstName,
            LastName   = user.LastName,
            Role       = user.Role,
            Technology = string.IsNullOrWhiteSpace(user.Technology)
                ? null
                : user.Technology.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };

        return ApiResponse<UserProfileDto>.Ok(profileDto, "Profile retrieved successfully.");
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(UpdateProfileRequestDto request)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return ApiResponse<UserProfileDto>.Fail("User not authenticated.");
        }

        var user = await _userRepository.GetByIdAsync<User>(userId);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("User not found.");
        }

        // Apply only non-null fields — null means "leave unchanged"
        if (request.FirstName != null)
            user.FirstName = request.FirstName;

        if (request.LastName != null)
            user.LastName = request.LastName;

        if (request.Role != null)
            user.Role = request.Role;

        if (request.Technology != null)
            user.Technology = request.Technology.Any()
                ? string.Join(",", request.Technology)
                : null;

        user.UpdatedBy = userId;

        await _userRepository.UpsertAsync(user);

        // Build the response DTO from the updated entity
        var profileDto = new UserProfileDto
        {
            FirstName  = user.FirstName,
            LastName   = user.LastName,
            Role       = user.Role,
            Technology = string.IsNullOrWhiteSpace(user.Technology)
                ? null
                : user.Technology.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };

        return ApiResponse<UserProfileDto>.Ok(profileDto, "Profile updated successfully.");
    }
}
