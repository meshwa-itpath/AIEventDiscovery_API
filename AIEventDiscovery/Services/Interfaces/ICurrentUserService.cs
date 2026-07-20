using System;

namespace AIEventDiscovery.Services.Interfaces;

/// <summary>
/// Service contract to retrieve details about the currently authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the currently authenticated user, or Guid.Empty if not authenticated.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the email of the currently authenticated user.
    /// </summary>
    string? Email { get; }
}
