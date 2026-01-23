using System.Text.Json;
using System.Text.RegularExpressions;
using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Helpers;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of repository fetching logic
/// </summary>
public class RepositoryFetcher : IRepositoryFetcher
{
    private readonly GitHubOptions _options;
    private readonly GitHubHttpClientHelper _clientHelper;
    private readonly ILogger<RepositoryFetcher> _logger;

    public RepositoryFetcher(
        IOptions<GitHubOptions> options,
        GitHubHttpClientHelper clientHelper,
        ILogger<RepositoryFetcher> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _clientHelper = clientHelper ?? throw new ArgumentNullException(nameof(clientHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> FetchRepositoryCountAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var url = GitHubApiEndpoints.GetOrgRepositoriesForCount(_options.Organization);
        var response = await client.GetAsync(url, cancellationToken);
        var content = await _clientHelper.HandleResponseAsync(response, cancellationToken);

        // Check if there's pagination
        if (response.Headers.TryGetValues("Link", out var linkHeaders))
        {
            var linkHeader = linkHeaders.FirstOrDefault();
            if (!string.IsNullOrEmpty(linkHeader))
            {
                // Parse the last page number from Link header
                var match = Regex.Match(linkHeader, @"page=(\d+)>; rel=""last""");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var lastPage))
                {
                    _logger.LogDebug("Found {PageCount} pages of repositories", lastPage);
                    return lastPage;
                }
            }
        }

        // No pagination, fetch all repos in one call
        _logger.LogDebug("No pagination detected, fetching all repositories in one call");
        var allReposUrl = GitHubApiEndpoints.GetOrgRepositories(_options.Organization, 1, 100);
        var allReposResponse = await client.GetAsync(allReposUrl, cancellationToken);
        var allContent = await _clientHelper.HandleResponseAsync(allReposResponse, cancellationToken);

        var repos = JsonSerializer.Deserialize<GitHubRepository[]>(allContent);
        return repos?.Length ?? 0;
    }

    /// <inheritdoc />
    public async Task<List<GitHubRepository>> FetchAllRepositoriesAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var allRepos = new List<GitHubRepository>();
        var page = 1;
        const int perPage = 100; // Maximum allowed by GitHub API
        var maxRepos = _options.MaxRepositories;

        if (maxRepos > 0)
        {
            _logger.LogInformation("Repository limit set to {MaxRepos}", maxRepos);
        }
        else
        {
            _logger.LogInformation("Fetching all repositories (no limit)");
        }

        while (true)
        {
            // Check if we've reached the limit
            if (maxRepos > 0 && allRepos.Count >= maxRepos)
            {
                _logger.LogInformation("Reached repository limit of {MaxRepos}", maxRepos);
                break;
            }

            var url = GitHubApiEndpoints.GetOrgRepositories(_options.Organization, page, perPage);
            var response = await client.GetAsync(url, cancellationToken);
            var content = await _clientHelper.HandleResponseAsync(response, cancellationToken);

            var repos = JsonSerializer.Deserialize<GitHubRepository[]>(content);

            if (repos == null || repos.Length == 0)
            {
                break; // No more repos
            }

            // If we have a limit, only add repos up to the limit
            if (maxRepos > 0)
            {
                var remaining = maxRepos - allRepos.Count;
                var reposToAdd = repos.Take(remaining).ToList();
                allRepos.AddRange(reposToAdd);
                
                _logger.LogDebug("Fetched page {Page} with {Count} repositories (added {Added}, total: {Total})", 
                    page, repos.Length, reposToAdd.Count, allRepos.Count);
            }
            else
            {
                allRepos.AddRange(repos);
                _logger.LogDebug("Fetched page {Page} with {Count} repositories (total: {Total})", 
                    page, repos.Length, allRepos.Count);
            }

            // If we got less than perPage, we've reached the end
            if (repos.Length < perPage)
            {
                break;
            }

            // If we have a limit and we've reached it, stop
            if (maxRepos > 0 && allRepos.Count >= maxRepos)
            {
                break;
            }

            page++;
        }

        var limitNote = maxRepos > 0 ? $" (limited to {maxRepos})" : "";
        _logger.LogInformation("Fetched {Count} repositories across {Pages} pages{LimitNote}", 
            allRepos.Count, page, limitNote);
        return allRepos;
    }
}
