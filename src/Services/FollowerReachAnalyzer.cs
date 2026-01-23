using System.Text.Json;
using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of follower reach analyzer
/// </summary>
public class FollowerReachAnalyzer : IFollowerReachAnalyzer
{
    private readonly GitHubOptions _options;
    private readonly ICachingService _cachingService;
    private readonly IGitHubApiClient _apiClient;
    private readonly IRepositoryFetcher _repositoryFetcher;
    private readonly ILogger<FollowerReachAnalyzer> _logger;

    public FollowerReachAnalyzer(
        IOptions<GitHubOptions> options,
        ICachingService cachingService,
        IGitHubApiClient apiClient,
        IRepositoryFetcher repositoryFetcher,
        ILogger<FollowerReachAnalyzer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repositoryFetcher = repositoryFetcher ?? throw new ArgumentNullException(nameof(repositoryFetcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<FollowerReach> GetFollowerReachAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeys.FollowerReach}_{_options.Organization}";

        if (_cachingService.TryGetValue<FollowerReach>(cacheKey, out var cachedReach) && cachedReach != null)
        {
            _logger.LogInformation("Returning cached follower reach for organization {Organization}", _options.Organization);
            return cachedReach;
        }

        _logger.LogInformation("Fetching follower reach from GitHub API for organization {Organization}", _options.Organization);

        try
        {
            var client = _apiClient.CreateClient();

            var allRepos = await _repositoryFetcher.FetchAllRepositoriesAsync(client, cancellationToken);

            var orgMembers = await FetchOrganizationMembersAsync(client, cancellationToken);
            var orgMemberLogins = new HashSet<string>(orgMembers, StringComparer.OrdinalIgnoreCase);

            var limit = _options.MaxRepositoriesForContributorAnalysis;
            _logger.LogInformation(
                "Analyzing top {Limit} repositories (from {Total} total) for follower reach",
                limit, allRepos.Count);

            var reposToCheck = allRepos
                .OrderByDescending(r => r.Stargazers_Count + r.Forks_Count + r.Open_Issues_Count)
                .Take(Math.Min(limit, allRepos.Count))
                .ToList();

            var allContributors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var maxConcurrency = 10;
            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var contributorLock = new object();

            var contributorTasks = reposToCheck.Select(async repo =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var contributors = await FetchRepositoryContributorsAsync(client, repo.Name, cancellationToken);
                    lock (contributorLock)
                    {
                        foreach (var contributor in contributors)
                        {
                            allContributors.Add(contributor);
                        }
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

            await Task.WhenAll(contributorTasks);

            _logger.LogInformation("Found {Count} unique contributors to analyze for follower reach", allContributors.Count);

            var totalFollowers = 0;
            var successCount = 0;
            var failCount = 0;
            var followerLock = new object();

            using var followerSemaphore = new SemaphoreSlim(maxConcurrency);

            var followerTasks = allContributors.Select(async username =>
            {
                await followerSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var followers = await FetchUserFollowersAsync(client, username, cancellationToken);
                    lock (followerLock)
                    {
                        totalFollowers += followers;
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to fetch followers for user {Username}", username);
                    lock (followerLock)
                    {
                        failCount++;
                    }
                }
                finally
                {
                    followerSemaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(followerTasks);

            var reach = new FollowerReach
            {
                TotalFollowers = totalFollowers,
                ContributorsAnalyzed = successCount,
                ContributorsFailed = failCount,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Successfully calculated follower reach: {Followers} total followers from {Success} contributors ({Failed} failed)",
                totalFollowers,
                successCount,
                failCount);

            _cachingService.Set(cacheKey, reach);
            return reach;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch follower reach for organization {Organization}", _options.Organization);
            throw;
        }
    }

    private async Task<int> FetchUserFollowersAsync(HttpClient client, string username, CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/users/{username}";
        var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch user {username}: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var userProfile = JsonSerializer.Deserialize<GitHubUser>(content);

        return userProfile?.Followers ?? 0;
    }

    private async Task<List<string>> FetchOrganizationMembersAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var allMembers = new List<string>();
        var page = 1;
        var hasMorePages = true;

        while (hasMorePages)
        {
            var url = $"https://api.github.com/orgs/{_options.Organization}/members?page={page}&per_page=100";
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch organization members page {Page}: {StatusCode}", page, response.StatusCode);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var members = JsonSerializer.Deserialize<List<GitHubUser>>(content);

            if (members == null || members.Count == 0)
            {
                hasMorePages = false;
            }
            else
            {
                allMembers.AddRange(members.Select(m => m.Login));
                page++;
            }
        }

        return allMembers;
    }

    private async Task<List<string>> FetchRepositoryContributorsAsync(HttpClient client, string repoName, CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_options.Organization}/{repoName}/contributors?per_page=100";
        var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new List<string>();
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var contributors = JsonSerializer.Deserialize<List<GitHubContributor>>(content);

        return contributors?.Select(c => c.Login).ToList() ?? new List<string>();
    }
}
