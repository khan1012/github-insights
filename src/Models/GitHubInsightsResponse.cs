namespace GitHubInsights.Models;

/// <summary>
/// Response model for GitHub insights data
/// </summary>
public class GitHubInsightsResponse
{
    /// <summary>
    /// The GitHub organization name
    /// </summary>
    public required string Organization { get; init; }

    /// <summary>
    /// Total number of repositories in the organization
    /// </summary>
    public int TotalRepositories { get; init; }

    /// <summary>
    /// Timestamp when the data was retrieved
    /// </summary>
    public DateTime Timestamp { get; init; }
}
