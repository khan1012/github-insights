namespace GitHubInsights.Models;

/// <summary>
/// Represents the estimated follower reach for all contributors
/// </summary>
public class FollowerReach
{
    /// <summary>
    /// Total follower count across all contributors
    /// </summary>
    public int TotalFollowers { get; set; }

    /// <summary>
    /// Number of contributors analyzed for follower count
    /// </summary>
    public int ContributorsAnalyzed { get; set; }

    /// <summary>
    /// Number of contributors that failed to fetch (deleted/private accounts)
    /// </summary>
    public int ContributorsFailed { get; set; }

    /// <summary>
    /// Timestamp when this data was retrieved
    /// </summary>
    public DateTime Timestamp { get; set; }
}
