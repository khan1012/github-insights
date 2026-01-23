namespace GitHubInsights.Services;

/// <summary>
/// Service responsible for fetching pull request data from GitHub API
/// </summary>
public interface IPullRequestFetcher
{
    /// <summary>
    /// Fetches pull request counts for an organization (open and closed)
    /// </summary>
    Task<(int openPRs, int closedPRs)> FetchPullRequestCountsAsync(
        HttpClient client,
        CancellationToken cancellationToken = default);
}
