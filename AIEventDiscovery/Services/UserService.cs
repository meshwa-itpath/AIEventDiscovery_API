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
}
