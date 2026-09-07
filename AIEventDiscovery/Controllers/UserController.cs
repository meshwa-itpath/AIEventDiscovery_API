using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIEventDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("SaveOnBoardingDetail")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveOnBoardingDetail([FromBody] OnBoardingDetailRequestDto request)
    {
        var response = await _userService.SaveOnBoardingDetailAsync(request);

        return response.Success
            ? Ok(response)
            : NotFound(response);
    }

    [HttpGet("GetProfile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var response = await _userService.GetProfileAsync();

        return response.Success
            ? Ok(response)
            : NotFound(response);
    }

    [HttpPut("UpdateProfile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var response = await _userService.UpdateProfileAsync(request);

        return response.Success
            ? Ok(response)
            : NotFound(response);
    }
}
