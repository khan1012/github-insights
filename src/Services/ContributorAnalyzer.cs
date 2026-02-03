using System.Collections.Concurrent;
using System.Text.Json;
using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of contributor analysis logic with parallel processing
/// </summary>
public class ContributorAnalyzer : IContributorAnalyzer
{
    private readonly GitHubOptions _options;
    private readonly PerformanceOptions _performanceOptions;
    private readonly ILogger<ContributorAnalyzer> _logger;

    public ContributorAnalyzer(
        IOptions<GitHubOptions> options,
        IOptions<PerformanceOptions> performanceOptions,
        ILogger<ContributorAnalyzer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _performanceOptions = performanceOptions?.Value ?? throw new ArgumentNullException(nameof(performanceOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<(int internalCount, int externalCount)> FetchContributorCountsAsync(
        HttpClient client,
        List<GitHubRepository> repositories,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, fetch organization members
            var orgMembers = await FetchOrganizationMembersAsync(client, cancellationToken);
            var orgMemberLogins = new HashSet<string>(orgMembers, StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("Found {Count} organization members", orgMemberLogins.Count);

            // Strategy: Sample top repos for speed while maintaining accuracy
            var limit = _options.MaxRepositoriesForContributorAnalysis;
            var reposToCheck = repositories
                .OrderByDescending(r => r.Stargazers_Count + r.Forks_Count + r.Open_Issues_Count)
                .Take(Math.Min(limit, repositories.Count))
                .ToList();

            _logger.LogInformation("Fetching contributors from top {Sample} of {Total} repositories using parallel processing...",
                reposToCheck.Count, repositories.Count);

            // Collect all unique contributors across sampled repositories
            var allContributors = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            // Use SemaphoreSlim to limit concurrent requests
            using var semaphore = new SemaphoreSlim(_performanceOptions.MaxConcurrentRequests);

            var tasks = reposToCheck.Select(async repo =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var contributors = await FetchRepositoryContributorsAsync(client, repo.Name, cancellationToken);
                    foreach (var contributor in contributors)
                    {
                        allContributors.TryAdd(contributor, 0);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to fetch contributors for repository {Repo}", repo.Name);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            // Split contributors into internal and external
            var contributorList = allContributors.Keys.ToList();
            var internalCount = contributorList.Count(c => orgMemberLogins.Contains(c));
            var externalCount = contributorList.Count - internalCount;

            _logger.LogInformation(
                "Found {Total} unique contributors across all repositories: {Internal} internal, {External} external",
                contributorList.Count,
                internalCount,
                externalCount);

            return (internalCount, externalCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch contributor counts, returning zeros");
            return (0, 0);
        }
    }

    /// <summary>
    /// Fetches organization members with pagination
    /// </summary>
    private async Task<List<string>> FetchOrganizationMembersAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var members = new List<string>();
        var page = 1;
        const int perPage = 100;

        try
        {
            while (true)
            {
                var url = GitHubApiEndpoints.GetOrgMembers(_options.Organization, page, perPage);
                var response = await client.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch organization members, status: {Status}", response.StatusCode);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var memberData = JsonSerializer.Deserialize<JsonElement[]>(content);

                if (memberData == null || memberData.Length == 0)
                {
                    break;
                }

                foreach (var member in memberData)
                {
                    if (member.TryGetProperty("login", out var login))
                    {
                        members.Add(login.GetString() ?? "");
                    }
                }

                if (memberData.Length < perPage)
                {
                    break;
                }

                page++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching organization members");
        }

        return members;
    }

    /// <summary>
    /// Fetches contributors for a specific repository
    /// </summary>
    private async Task<List<string>> FetchRepositoryContributorsAsync(
        HttpClient client,
        string repoName,
        CancellationToken cancellationToken)
    {
        var contributors = new List<string>();

        try
        {
            var url = GitHubApiEndpoints.GetRepoContributors(_options.Organization, repoName);
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return contributors;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var contributorData = JsonSerializer.Deserialize<JsonElement[]>(content);

            if (contributorData != null)
            {
                foreach (var contributor in contributorData)
                {
                    if (contributor.TryGetProperty("login", out var login))
                    {
                        var loginStr = login.GetString();
                        if (!string.IsNullOrEmpty(loginStr))
                        {
                            contributors.Add(loginStr);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch contributors for {Repo}", repoName);
        }

        return contributors;
    }
}
