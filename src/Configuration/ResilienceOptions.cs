using System.ComponentModel.DataAnnotations;

namespace GitHubInsights.Configuration;

/// <summary>
/// Resilience and retry policy configuration
/// </summary>
public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    /// <summary>
    /// Maximum retry attempts for transient failures
    /// </summary>
    [Range(0, 10, ErrorMessage = "Max retries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay in seconds for exponential backoff
    /// </summary>
    [Range(1, 30, ErrorMessage = "Base delay must be between 1 and 30 seconds")]
    public int BaseDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Circuit breaker failure threshold before opening
    /// </summary>
    [Range(2, 20, ErrorMessage = "Failure threshold must be between 2 and 20")]
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker duration in seconds before attempting to close
    /// </summary>
    [Range(10, 300, ErrorMessage = "Break duration must be between 10 and 300 seconds")]
    public int CircuitBreakerDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Overall timeout for HTTP requests in seconds
    /// </summary>
    [Range(10, 300, ErrorMessage = "Timeout must be between 10 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;
}
