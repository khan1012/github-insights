namespace GitHubInsights.Services;

/// <summary>
/// Client for making GitHub API requests
/// </summary>
public interface IGitHubApiClient
{
    /// <summary>
    /// Create a configured HTTP client for GitHub API
    /// </summary>
    HttpClient CreateClient();
}
