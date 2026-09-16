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

        var primaryStacks = request.PrimaryStacks?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        user.PrimaryStacks = primaryStacks != null && primaryStacks.Count > 0
            ? string.Join(",", primaryStacks)
            : null;

        var interests = request.Interests?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        user.Interests = interests != null && interests.Count > 0
            ? string.Join(",", interests)
            : null;

        // // Synchronize legacy Technology column for backward compatibility
        // var allTech = new List<string>();
        // if (primaryStacks != null) allTech.AddRange(primaryStacks);
        // if (interests != null) allTech.AddRange(interests);
        // if (allTech.Count == 0 && request.Technology != null)
        // {
        //     allTech.AddRange(request.Technology.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        // }

        // user.Technology = allTech.Count > 0
        //     ? string.Join(",", allTech.Distinct(StringComparer.OrdinalIgnoreCase))
        //     : null;

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
            FirstName     = user.FirstName,
            LastName      = user.LastName,
            Role          = user.Role,
            PrimaryStacks = ParseCommaSeparated(user.PrimaryStacks),
            Interests     = ParseCommaSeparated(user.Interests),
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

        if (request.PrimaryStacks != null)
        {
            var primaryStacks = request.PrimaryStacks
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            user.PrimaryStacks = primaryStacks.Count > 0 ? string.Join(",", primaryStacks) : null;
        }

        if (request.Interests != null)
        {
            var interests = request.Interests
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            user.Interests = interests.Count > 0 ? string.Join(",", interests) : null;
        }

        user.UpdatedBy = userId;

        await _userRepository.UpsertAsync(user);

        // Build the response DTO from the updated entity
        var profileDto = new UserProfileDto
        {
            FirstName     = user.FirstName,
            LastName      = user.LastName,
            Role          = user.Role,
            PrimaryStacks = ParseCommaSeparated(user.PrimaryStacks),
            Interests     = ParseCommaSeparated(user.Interests),
        };

        return ApiResponse<UserProfileDto>.Ok(profileDto, "Profile updated successfully.");
    }

    private static List<string>? ParseCommaSeparated(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
