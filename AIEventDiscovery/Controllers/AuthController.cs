using System.Security.Claims;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIEventDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Registers a new user. Returns 201 on success, 409 if email already exists.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var response = await _authService.RegisterAsync(request);

        // Controller decides the HTTP status code; service only owns the message/data
        return response.Success
            ? StatusCode(StatusCodes.Status201Created, response)
            : Conflict(response);
    }

    /// <summary>Authenticates a user. Returns 200 with JWT on success, 401 on invalid credentials.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request);

        return response.Success
            ? Ok(response)
            : Unauthorized(response);
    }

    /// <summary>Returns the profile of the currently authenticated user. Requires Bearer token.</summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("Invalid token claims."));

        var response = await _authService.GetProfileAsync(userId);

        return response.Success
            ? Ok(response)
            : NotFound(response);
    }
}
