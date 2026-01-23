namespace GitHubInsights.Models;

/// <summary>
/// Detailed repository statistics response
/// </summary>
public class RepositoryDetailsResponse
{
    /// <summary>
    /// The GitHub organization name
    /// </summary>
    public required string Organization { get; init; }

    /// <summary>
    /// Total number of repositories
    /// </summary>
    public int TotalRepositories { get; init; }

    /// <summary>
    /// Total stars across all repositories
    /// </summary>
    public int TotalStars { get; init; }

    /// <summary>
    /// Total forks across all repositories
    /// </summary>
    public int TotalForks { get; init; }

    /// <summary>
    /// Total open issues across all repositories
    /// </summary>
    public int TotalOpenIssues { get; init; }

    /// <summary>
    /// Total watchers across all repositories
    /// </summary>
    public int TotalWatchers { get; init; }

    /// <summary>
    /// Total open pull requests across all repositories
    /// </summary>
    public int TotalOpenPullRequests { get; init; }

    /// <summary>
    /// Total closed/merged pull requests across all repositories
    /// </summary>
    public int TotalClosedPullRequests { get; init; }

    /// <summary>
    /// Total number of internal contributors (organization members)
    /// </summary>
    public int TotalInternalContributors { get; init; }

    /// <summary>
    /// Total number of external contributors (non-organization members)
    /// </summary>
    public int TotalExternalContributors { get; init; }

    /// <summary>
    /// Timestamp when the data was retrieved
    /// </summary>
    public DateTime Timestamp { get; init; }
}
