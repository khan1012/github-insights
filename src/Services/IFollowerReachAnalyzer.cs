using GitHubInsights.Models;

namespace GitHubInsights.Services;

/// <summary>
/// Analyzer for calculating follower reach
/// </summary>
public interface IFollowerReachAnalyzer
{
    /// <summary>
    /// Calculate the total follower reach across contributors
    /// </summary>
    Task<FollowerReach> GetFollowerReachAsync(CancellationToken cancellationToken = default);
}
