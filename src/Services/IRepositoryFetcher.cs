namespace GitHubInsights.Services;

/// <summary>
/// Service responsible for fetching repository data from GitHub API
/// </summary>
public interface IRepositoryFetcher
{
    /// <summary>
    /// Fetches all repositories for an organization with pagination
    /// </summary>
    Task<List<Models.GitHub.GitHubRepository>> FetchAllRepositoriesAsync(
        HttpClient client,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches repository count for an organization
    /// </summary>
    Task<int> FetchRepositoryCountAsync(
        HttpClient client,
        CancellationToken cancellationToken = default);
}
