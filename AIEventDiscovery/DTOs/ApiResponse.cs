namespace AIEventDiscovery.DTOs;

/// <summary>
/// Generic wrapper for all API responses.
/// Ensures a consistent shape: { success, message, data } across every endpoint.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public T? Data { get; set; }

    public ApiResponse() { }

    public ApiResponse(bool success, string message, T? data = default)
    {
        Success = success;
        Message = message;
        Data    = data;
    }

    // ── Static factory helpers so controllers stay one-liners ──────────

    /// <summary>Creates a successful response with data and an optional message.</summary>
    public static ApiResponse<T> Ok(T? data = default, string message = "Success.")
        => new(true, message, data);

    /// <summary>Creates a failure response with no data payload.</summary>
    public static ApiResponse<T> Fail(string message)
        => new(false, message, default);
}
