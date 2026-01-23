using System.Text.Json;
using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of pull request fetching logic
/// </summary>
public class PullRequestFetcher : IPullRequestFetcher
{
    private readonly GitHubOptions _options;
    private readonly ILogger<PullRequestFetcher> _logger;

    public PullRequestFetcher(
        IOptions<GitHubOptions> options,
        ILogger<PullRequestFetcher> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<(int openPRs, int closedPRs)> FetchPullRequestCountsAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch open PRs count using search API
            var openPRsUrl = GitHubApiEndpoints.SearchIssues(
                GitHubApiEndpoints.BuildPullRequestQuery(_options.Organization, "open"));
            var openPRsResponse = await client.GetAsync(openPRsUrl, cancellationToken);

            int openPRs = 0;
            if (openPRsResponse.IsSuccessStatusCode)
            {
                var openContent = await openPRsResponse.Content.ReadAsStringAsync(cancellationToken);
                var openData = JsonSerializer.Deserialize<GitHubSearchResponse>(openContent);
                openPRs = openData?.Total_Count ?? 0;
                _logger.LogDebug("Found {Count} open pull requests", openPRs);
            }

            // Fetch closed/merged PRs count using search API
            var closedPRsUrl = GitHubApiEndpoints.SearchIssues(
                GitHubApiEndpoints.BuildPullRequestQuery(_options.Organization, "closed"));
            var closedPRsResponse = await client.GetAsync(closedPRsUrl, cancellationToken);

            int closedPRs = 0;
            if (closedPRsResponse.IsSuccessStatusCode)
            {
                var closedContent = await closedPRsResponse.Content.ReadAsStringAsync(cancellationToken);
                var closedData = JsonSerializer.Deserialize<GitHubSearchResponse>(closedContent);
                closedPRs = closedData?.Total_Count ?? 0;
                _logger.LogDebug("Found {Count} closed/merged pull requests", closedPRs);
            }

            return (openPRs, closedPRs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch pull request counts, returning zeros");
            return (0, 0);
        }
    }
}
