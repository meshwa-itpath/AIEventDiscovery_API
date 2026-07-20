using System;
using System.Security.Claims;
using AIEventDiscovery.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AIEventDiscovery.Services;

/// <summary>
/// Reads the current user's identity from the HTTP request's JWT token claims.
/// Registered as a scoped service so it reflects the per-request HttpContext.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }

    /// <inheritdoc />
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
}
