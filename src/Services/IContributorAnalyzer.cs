namespace GitHubInsights.Services;

/// <summary>
/// Service responsible for fetching and analyzing contributor data
/// </summary>
public interface IContributorAnalyzer
{
    /// <summary>
    /// Fetches contributor counts, distinguishing between organization members and external contributors
    /// </summary>
    Task<(int internalCount, int externalCount)> FetchContributorCountsAsync(
        HttpClient client,
        List<Models.GitHub.GitHubRepository> repositories,
        CancellationToken cancellationToken = default);
}
