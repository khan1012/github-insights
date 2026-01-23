using GitHubInsights.Models;

namespace GitHubInsights.Services;

/// <summary>
/// Service interface for GitHub contributor and user operations
/// </summary>
public interface IGitHubContributorService
{
    /// <summary>
    /// Gets contributor statistics (may take longer)
    /// </summary>
    /// <returns>Contributor statistics</returns>
    Task<ContributorStats> GetContributorStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets estimated follower reach across all contributors
    /// </summary>
    /// <returns>Follower reach statistics</returns>
    Task<FollowerReach> GetFollowerReachAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top contributors with detailed information
    /// </summary>
    /// <returns>Top contributors list</returns>
    Task<TopContributors> GetTopContributorsAsync(CancellationToken cancellationToken = default);
}
