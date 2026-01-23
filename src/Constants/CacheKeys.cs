namespace GitHubInsights.Constants;

/// <summary>
/// Constants for cache keys used throughout the application
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Cache key for repository count
    /// </summary>
    public const string RepositoryCount = "GitHubRepoCount";

    /// <summary>
    /// Cache key for repository details
    /// </summary>
    public const string RepositoryDetails = "GitHubRepoDetails";

    /// <summary>
    /// Cache key for basic repository details (without contributors)
    /// </summary>
    public const string BasicRepositoryDetails = "GitHubBasicRepoDetails";

    /// <summary>
    /// Cache key for contributor statistics
    /// </summary>
    public const string ContributorStats = "GitHubContributorStats";

    /// <summary>
    /// Cache key for follower reach statistics
    /// </summary>
    public const string FollowerReach = "GitHubFollowerReach";

    /// <summary>
    /// Cache key for dependent repositories data
    /// </summary>
    public const string DependentRepositories = "GitHubDependentRepos";

    /// <summary>
    /// Cache key for detailed insights data
    /// </summary>
    public const string DetailedInsights = "GitHubDetailedInsights";

    /// <summary>
    /// Cache key for top contributors data
    /// </summary>
    public const string TopContributors = "GitHubTopContributors";
}
