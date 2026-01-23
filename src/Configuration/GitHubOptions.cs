using System.ComponentModel.DataAnnotations;

namespace GitHubInsights.Configuration;

/// <summary>
/// Strongly-typed configuration for GitHub API settings
/// </summary>
public class GitHubOptions
{
    public const string SectionName = "GitHub";

    /// <summary>
    /// GitHub organization name (required)
    /// </summary>
    [Required(ErrorMessage = "GitHub organization name is required")]
    [MinLength(1, ErrorMessage = "Organization name cannot be empty")]
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// GitHub Personal Access Token (optional, but recommended for higher rate limits)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Cache duration in minutes for GitHub API responses
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Cache duration must be between 1 and 1440 minutes")]
    public int CacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum number of repositories to fetch (optional, 0 = unlimited)
    /// Useful for large organizations to reduce loading time
    /// </summary>
    [Range(0, 10000, ErrorMessage = "Max repositories must be between 0 and 1000")]
    public int MaxRepositories { get; set; } = 100;

    /// <summary>
    /// Maximum number of top repositories to analyze for contributors
    /// Reduces API calls for large organizations. Default: 20
    /// </summary>
    [Range(5, 100, ErrorMessage = "Max repositories for contributor analysis must be between 5 and 100")]
    public int MaxRepositoriesForContributorAnalysis { get; set; } = 20;
}
