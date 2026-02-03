using System.Collections.Concurrent;
using System.Text.Json;
using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of top contributors analyzer
/// </summary>
public class TopContributorsAnalyzer : ITopContributorsAnalyzer
{
    private readonly GitHubOptions _options;
    private readonly ICachingService _cachingService;
    private readonly IGitHubApiClient _apiClient;
    private readonly IRepositoryFetcher _repositoryFetcher;
    private readonly ILogger<TopContributorsAnalyzer> _logger;

    public TopContributorsAnalyzer(
        IOptions<GitHubOptions> options,
        ICachingService cachingService,
        IGitHubApiClient apiClient,
        IRepositoryFetcher repositoryFetcher,
        ILogger<TopContributorsAnalyzer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repositoryFetcher = repositoryFetcher ?? throw new ArgumentNullException(nameof(repositoryFetcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TopContributors> GetTopContributorsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeys.TopContributors}";

        if (_cachingService.TryGetValue(cacheKey, out TopContributors? cachedData))
        {
            _logger.LogInformation(
                "Returning cached top contributors for organization {Organization}",
                _options.Organization);
            return cachedData!;
        }

        _logger.LogInformation(
            "Fetching top contributors from GitHub API for organization {Organization}",
            _options.Organization);

        try
        {
            var client = _apiClient.CreateClient();

            // Get organization members first
            var orgMembers = await FetchOrganizationMembersAsync(client, cancellationToken);
            var orgMemberSet = new HashSet<string>(orgMembers, StringComparer.OrdinalIgnoreCase);

            // Fetch repositories
            var allRepos = await _repositoryFetcher.FetchAllRepositoriesAsync(client, cancellationToken);

            // Get top repositories to sample from (configurable limit)
            var reposToAnalyze = _options.MaxRepositoriesForContributorAnalysis;
            _logger.LogInformation(
                "Analyzing top {Count} repositories for contributors (out of {Total} repos)",
                reposToAnalyze, allRepos.Count);

            var topRepos = allRepos
                .OrderByDescending(r => r.Stargazers_Count + r.Forks_Count)
                .Take(reposToAnalyze)
                .ToList();

            // Collect contributors with their contribution counts
            var contributorStats = new ConcurrentDictionary<string, (int contributions, HashSet<string> repos)>();
            var maxConcurrency = 10;
            using var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = topRepos.Select(async repo =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var url = $"https://api.github.com/repos/{_options.Organization}/{repo.Name}/contributors?per_page=100";
                    var response = await client.GetAsync(url, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        var contributors = JsonSerializer.Deserialize<List<GitHubContributor>>(content);

                        if (contributors != null)
                        {
                            foreach (var contributor in contributors)
                            {
                                contributorStats.AddOrUpdate(
                                    contributor.Login,
                                    (contributor.Contributions, new HashSet<string> { repo.Name }),
                                    (key, existing) =>
                                    {
                                        existing.repos.Add(repo.Name);
                                        return (existing.contributions + contributor.Contributions, existing.repos);
                                    });
                            }
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

            await Task.WhenAll(tasks);

            // Get top 10 contributors by contribution count
            var topContributorsData = contributorStats
                .OrderByDescending(x => x.Value.contributions)
                .Take(10)
                .ToList();

            // Fetch detailed info for top contributors
            var contributors = new List<ContributorDetail>();

            using var detailSemaphore = new SemaphoreSlim(5);
            var detailTasks = topContributorsData.Select(async kvp =>
            {
                await detailSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var url = $"https://api.github.com/users/{kvp.Key}";
                    var response = await client.GetAsync(url, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        var user = JsonSerializer.Deserialize<JsonElement>(content);

                        return new ContributorDetail
                        {
                            Username = kvp.Key,
                            ProfileUrl = user.GetProperty("html_url").GetString() ?? "",
                            AvatarUrl = user.TryGetProperty("avatar_url", out var avatar) ? avatar.GetString() : null,
                            TotalContributions = kvp.Value.contributions,
                            RepositoriesContributedTo = kvp.Value.repos.Count,
                            Followers = user.TryGetProperty("followers", out var followers) ? followers.GetInt32() : 0,
                            IsInternal = orgMemberSet.Contains(kvp.Key),
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to fetch user details for {Username}", kvp.Key);
                }
                finally
                {
                    detailSemaphore.Release();
                }

                // Fallback if API call fails
                return new ContributorDetail
                {
                    Username = kvp.Key,
                    ProfileUrl = $"https://github.com/{kvp.Key}",
                    TotalContributions = kvp.Value.contributions,
                    RepositoriesContributedTo = kvp.Value.repos.Count,
                    Followers = 0,
                    IsInternal = orgMemberSet.Contains(kvp.Key),
                };
            }).ToList();

            var detailedContributors = await Task.WhenAll(detailTasks);
            contributors.AddRange(detailedContributors.Where(c => c != null)!);

            var result = new TopContributors
            {
                Organization = _options.Organization,
                Contributors = contributors,
                Timestamp = DateTime.UtcNow,
            };

            _cachingService.Set(cacheKey, result);

            _logger.LogInformation("Successfully fetched top {Count} contributors", contributors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching top contributors for organization {Organization}",
                _options.Organization);
            throw new InvalidOperationException($"Failed to fetch top contributors: {ex.Message}", ex);
        }
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
}
