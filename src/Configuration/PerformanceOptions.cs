using System.ComponentModel.DataAnnotations;

namespace GitHubInsights.Configuration;

/// <summary>
/// Performance and concurrency settings
/// </summary>
public class PerformanceOptions
{
    public const string SectionName = "Performance";

    /// <summary>
    /// Maximum concurrent GitHub API requests
    /// </summary>
    [Range(1, 50, ErrorMessage = "Max concurrent requests must be between 1 and 50")]
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Repository health check - days before considered stale
    /// </summary>
    [Range(30, 730, ErrorMessage = "Stale days must be between 30 and 730")]
    public int HealthCheckStaleDays { get; set; } = 180;

    /// <summary>
    /// Repository health check - days before needs attention
    /// </summary>
    [Range(7, 365, ErrorMessage = "Attention days must be between 7 and 365")]
    public int HealthCheckAttentionDays { get; set; } = 30;

    /// <summary>
    /// Activity score weight for stars
    /// </summary>
    public int ActivityScoreStarsWeight { get; set; } = 10;

    /// <summary>
    /// Activity score weight for forks
    /// </summary>
    public int ActivityScoreForksWeight { get; set; } = 5;

    /// <summary>
    /// Activity score weight for open issues
    /// </summary>
    public int ActivityScoreIssuesWeight { get; set; } = 2;

    /// <summary>
    /// Activity score bonus for recently updated repos
    /// </summary>
    public int ActivityScoreRecentUpdateBonus { get; set; } = 1000;
}
