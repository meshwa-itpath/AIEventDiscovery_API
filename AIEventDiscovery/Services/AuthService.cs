using AIEventDiscovery.Data.Repositories;
using AIEventDiscovery.DTOs;
using AIEventDiscovery.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AIEventDiscovery.Services;

/// <summary>
/// Handles user authentication: registration, login, and profile retrieval.
/// Uses BCrypt for secure password hashing and JWT for stateless auth tokens.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IGenericRepository<User> userRepository, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> RegisterAsync(RegisterRequestDto request)
    {
        var exists = await _userRepository.IsExistsAsync(u => u.Email == request.Email.ToLower());
        if (exists)
        {
            return ApiResponse<bool>.Fail($"An account with email '{request.Email}' already exists.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = passwordHash,
            IsActive = true
        };

        await _userRepository.UpsertAsync(user);
        return ApiResponse<bool>.Ok(true, "Account created successfully.");
    }

    public async Task<ApiResponse<string>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.FindAsync(u => u.Email == request.Email.ToLower(), useAsNoTracking: true);

        if (user == null)
        {
            return ApiResponse<string>.Fail("Invalid email or password.");
        }

        var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isValid)
        {
            return ApiResponse<string>.Fail("Invalid email or password.");
        }

        var token = GenerateJwtToken(user);
        return ApiResponse<string>.Ok(token, "Login successful.");
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId)
    {
        var profile = await _userRepository.GetByIdAsync(
            userId,
            selector: u => new UserProfileDto
            {
                FirstName = u.FirstName,
                LastName = u.LastName
            });

        if (profile == null)
            return ApiResponse<UserProfileDto>.Fail("User not found.");

        return ApiResponse<UserProfileDto>.Ok(profile, "Profile fetched successfully.");
    }

    #region Private helpers
    /// <summary>
    /// Generates a signed JWT token for the given user.
    /// Claims include the user's ID and email for easy extraction in controllers.
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"]!;
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
