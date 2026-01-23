namespace GitHubInsights.Constants;

/// <summary>
/// Constants for GitHub API endpoints
/// </summary>
public static class GitHubApiEndpoints
{
    private const string BaseUrl = "https://api.github.com";

    /// <summary>
    /// Gets the organization repositories endpoint
    /// </summary>
    public static string GetOrgRepositories(string organization, int page = 1, int perPage = 100) =>
        $"{BaseUrl}/orgs/{organization}/repos?page={page}&per_page={perPage}&sort=updated";

    /// <summary>
    /// Gets the organization repositories endpoint for pagination check
    /// </summary>
    public static string GetOrgRepositoriesForCount(string organization) =>
        $"{BaseUrl}/orgs/{organization}/repos?per_page=1";

    /// <summary>
    /// Gets the organization endpoint
    /// </summary>
    public static string GetOrganization(string organization) =>
        $"{BaseUrl}/orgs/{organization}";

    /// <summary>
    /// Search issues/PRs endpoint
    /// </summary>
    public static string SearchIssues(string query) =>
        $"{BaseUrl}/search/issues?q={query}&per_page=1";

    /// <summary>
    /// Builds a pull request search query
    /// </summary>
    public static string BuildPullRequestQuery(string organization, string state) =>
        $"type:pr+state:{state}+org:{organization}";

    /// <summary>
    /// Gets search query for open PRs in organization
    /// </summary>
    public static string GetOpenPRsQuery(string organization) =>
        $"type:pr+state:open+org:{organization}";

    /// <summary>
    /// Gets search query for closed PRs in organization
    /// </summary>
    public static string GetClosedPRsQuery(string organization) =>
        $"type:pr+state:closed+org:{organization}";

    /// <summary>
    /// Gets the organization members endpoint
    /// </summary>
    public static string GetOrgMembers(string organization, int page = 1, int perPage = 100) =>
        $"{BaseUrl}/orgs/{organization}/members?page={page}&per_page={perPage}";

    /// <summary>
    /// Gets the repository contributors endpoint
    /// </summary>
    public static string GetRepoContributors(string organization, string repoName, int perPage = 100) =>
        $"{BaseUrl}/repos/{organization}/{repoName}/contributors?per_page={perPage}";
}
