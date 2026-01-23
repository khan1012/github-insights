namespace GitHubInsights.Models;

/// <summary>
/// Standard error response model
/// </summary>
public class ApiError
{
    /// <summary>
    /// Error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Detailed error information (only in development)
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Timestamp of the error
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
