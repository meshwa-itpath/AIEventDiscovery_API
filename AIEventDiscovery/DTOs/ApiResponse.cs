namespace AIEventDiscovery.DTOs;

/// <summary>
/// Generic wrapper for all API responses.
/// Ensures a consistent shape: { success, message, data, pagination } across every endpoint.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public T? Data { get; set; }

    // ── Pagination metadata (null when not a paginated response) ──────
    public int? TotalRecords { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public int? TotalPages { get; set; }

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

    /// <summary>Creates a successful paginated response with full pagination metadata.</summary>
    public static ApiResponse<T> Paginated(T? data, int totalRecords, int page, int pageSize, string message = "Success.")
        => new(true, message, data)
        {
            TotalRecords = totalRecords,
            Page         = page,
            PageSize     = pageSize,
            TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize)
        };
}
