using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Service for GitHub contributor and user operations
/// </summary>
public class GitHubContributorService : IGitHubContributorService
{
    private readonly GitHubOptions _options;
    private readonly ICachingService _cachingService;
    private readonly IGitHubApiClient _apiClient;
    private readonly ILogger<GitHubContributorService> _logger;
    private readonly IRepositoryFetcher _repositoryFetcher;
    private readonly IContributorAnalyzer _contributorAnalyzer;
    private readonly ITopContributorsAnalyzer _topContributorsAnalyzer;
    private readonly IFollowerReachAnalyzer _followerReachAnalyzer;

    public GitHubContributorService(
        IOptions<GitHubOptions> options,
        ICachingService cachingService,
        IGitHubApiClient apiClient,
        ILogger<GitHubContributorService> logger,
        IRepositoryFetcher repositoryFetcher,
        IContributorAnalyzer contributorAnalyzer,
        ITopContributorsAnalyzer topContributorsAnalyzer,
        IFollowerReachAnalyzer followerReachAnalyzer)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryFetcher = repositoryFetcher ?? throw new ArgumentNullException(nameof(repositoryFetcher));
        _contributorAnalyzer = contributorAnalyzer ?? throw new ArgumentNullException(nameof(contributorAnalyzer));
        _topContributorsAnalyzer = topContributorsAnalyzer ?? throw new ArgumentNullException(nameof(topContributorsAnalyzer));
        _followerReachAnalyzer = followerReachAnalyzer ?? throw new ArgumentNullException(nameof(followerReachAnalyzer));
    }

    /// <inheritdoc />
    public async Task<ContributorStats> GetContributorStatsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachingService.TryGetValue(CacheKeys.ContributorStats, out ContributorStats? cachedStats))
        {
            _logger.LogInformation("Returning cached contributor stats for organization {Organization}", _options.Organization);
            return cachedStats!;
        }

        _logger.LogInformation("Fetching contributor statistics from GitHub API for organization {Organization}", _options.Organization);

        using var client = _apiClient.CreateClient();

        try
        {
            var allRepos = await _repositoryFetcher.FetchAllRepositoriesAsync(client, cancellationToken);
            var (internalContributors, externalContributors) = await _contributorAnalyzer.FetchContributorCountsAsync(client, allRepos, cancellationToken);

            var response = new ContributorStats
            {
                TotalInternalContributors = internalContributors,
                TotalExternalContributors = externalContributors,
                Timestamp = DateTime.UtcNow
            };

            _cachingService.Set(CacheKeys.ContributorStats, response);

            _logger.LogInformation(
                "Successfully fetched contributor stats: {Internal} internal, {External} external for organization {Organization}",
                internalContributors,
                externalContributors,
                _options.Organization);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching contributor stats for organization {Organization}", _options.Organization);
            throw new InvalidOperationException($"Failed to fetch contributor stats from GitHub: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching contributor stats for organization {Organization}", _options.Organization);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FollowerReach> GetFollowerReachAsync(CancellationToken cancellationToken = default)
    {
        return await _followerReachAnalyzer.GetFollowerReachAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TopContributors> GetTopContributorsAsync(CancellationToken cancellationToken = default)
    {
        return await _topContributorsAnalyzer.GetTopContributorsAsync(cancellationToken);
    }
}
