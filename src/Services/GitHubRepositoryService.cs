using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Service for GitHub repository operations
/// </summary>
public class GitHubRepositoryService : IGitHubRepositoryService
{
    private readonly GitHubOptions _options;
    private readonly ICachingService _cachingService;
    private readonly IGitHubApiClient _apiClient;
    private readonly ILogger<GitHubRepositoryService> _logger;
    private readonly IRepositoryFetcher _repositoryFetcher;
    private readonly IPullRequestFetcher _pullRequestFetcher;
    private readonly IRepositoryDependencyAnalyzer _dependencyAnalyzer;
    private readonly IRepositoryHealthAnalyzer _healthAnalyzer;

    public GitHubRepositoryService(
        IOptions<GitHubOptions> options,
        ICachingService cachingService,
        IGitHubApiClient apiClient,
        ILogger<GitHubRepositoryService> logger,
        IRepositoryFetcher repositoryFetcher,
        IPullRequestFetcher pullRequestFetcher,
        IRepositoryDependencyAnalyzer dependencyAnalyzer,
        IRepositoryHealthAnalyzer healthAnalyzer)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryFetcher = repositoryFetcher ?? throw new ArgumentNullException(nameof(repositoryFetcher));
        _pullRequestFetcher = pullRequestFetcher ?? throw new ArgumentNullException(nameof(pullRequestFetcher));
        _dependencyAnalyzer = dependencyAnalyzer ?? throw new ArgumentNullException(nameof(dependencyAnalyzer));
        _healthAnalyzer = healthAnalyzer ?? throw new ArgumentNullException(nameof(healthAnalyzer));
    }

    /// <inheritdoc />
    public async Task<GitHubInsightsResponse> GetRepositoryCountAsync(CancellationToken cancellationToken = default)
    {
        if (_cachingService.TryGetValue(CacheKeys.RepositoryCount, out GitHubInsightsResponse? cachedResponse))
        {
            _logger.LogInformation(
                "Returning cached repository count for organization {Organization}",
                _options.Organization);
            return cachedResponse!;
        }

        _logger.LogInformation(
            "Fetching repository count from GitHub API for organization {Organization}",
            _options.Organization);

        using var client = _apiClient.CreateClient();

        try
        {
            var totalRepos = await _repositoryFetcher.FetchRepositoryCountAsync(client, cancellationToken);

            var response = new GitHubInsightsResponse
            {
                Organization = _options.Organization,
                TotalRepositories = totalRepos,
                Timestamp = DateTime.UtcNow
            };

            _cachingService.Set(CacheKeys.RepositoryCount, response);

            _logger.LogInformation(
                "Successfully fetched {Count} repositories for organization {Organization}",
                totalRepos,
                _options.Organization);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error while fetching GitHub data for organization {Organization}",
                _options.Organization);
            throw new InvalidOperationException($"Failed to fetch data from GitHub: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while fetching GitHub data for organization {Organization}",
                _options.Organization);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RepositoryDetailsResponse> GetRepositoryDetailsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachingService.TryGetValue(CacheKeys.RepositoryDetails, out RepositoryDetailsResponse? cachedDetails))
        {
            _logger.LogInformation("Returning cached repository details for organization {Organization}", _options.Organization);
            return cachedDetails!;
        }

        _logger.LogInformation("Fetching detailed repository data from GitHub API for organization {Organization}", _options.Organization);

        using var client = _apiClient.CreateClient();

        try
        {
            var allRepos = await _repositoryFetcher.FetchAllRepositoriesAsync(client, cancellationToken);
            var (openPRs, closedPRs) = await _pullRequestFetcher.FetchPullRequestCountsAsync(client, cancellationToken);

            // For now, we don't fetch contributor stats in this method (they're in separate endpoints)
            var response = BuildRepositoryDetailsResponse(allRepos, openPRs, closedPRs, 0, 0);

            _cachingService.Set(CacheKeys.RepositoryDetails, response);

            _logger.LogInformation(
                "Successfully fetched details: {Count} repos, {Stars} total stars, {OpenPRs} open PRs for organization {Organization}",
                response.TotalRepositories,
                response.TotalStars,
                response.TotalOpenPullRequests,
                _options.Organization);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching detailed GitHub data for organization {Organization}", _options.Organization);
            throw new InvalidOperationException($"Failed to fetch detailed data from GitHub: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching detailed GitHub data for organization {Organization}", _options.Organization);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RepositoryDetailsResponse> GetBasicRepositoryDetailsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachingService.TryGetValue(CacheKeys.BasicRepositoryDetails, out RepositoryDetailsResponse? cachedDetails))
        {
            _logger.LogInformation("Returning cached basic repository details for organization {Organization}", _options.Organization);
            return cachedDetails!;
        }

        _logger.LogInformation("Fetching basic repository data (without contributors) from GitHub API for organization {Organization}", _options.Organization);

        using var client = _apiClient.CreateClient();

        try
        {
            var allRepos = await _repositoryFetcher.FetchAllRepositoriesAsync(client, cancellationToken);
            var (openPRs, closedPRs) = await _pullRequestFetcher.FetchPullRequestCountsAsync(client, cancellationToken);

            var response = BuildRepositoryDetailsResponse(allRepos, openPRs, closedPRs, 0, 0);

            _cachingService.Set(CacheKeys.BasicRepositoryDetails, response);

            _logger.LogInformation(
                "Successfully fetched basic details: {Count} repos, {Stars} total stars, {OpenPRs} open PRs for organization {Organization}",
                response.TotalRepositories,
                response.TotalStars,
                response.TotalOpenPullRequests,
                _options.Organization);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching basic GitHub data for organization {Organization}", _options.Organization);
            throw new InvalidOperationException($"Failed to fetch basic data from GitHub: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching basic GitHub data for organization {Organization}", _options.Organization);
            throw;
        }
    }



    private RepositoryDetailsResponse BuildRepositoryDetailsResponse(
        List<GitHubRepository> allRepos,
        int openPRs,
        int closedPRs,
        int internalContributors,
        int externalContributors)
    {
        var totalStars = allRepos.Sum(r => r.Stargazers_Count);
        var totalForks = allRepos.Sum(r => r.Forks_Count);
        var totalWatchers = allRepos.Sum(r => r.Watchers_Count);
        var totalOpenIssues = allRepos.Sum(r => r.Open_Issues_Count);

        return new RepositoryDetailsResponse
        {
            Organization = _options.Organization,
            TotalRepositories = allRepos.Count,
            TotalStars = totalStars,
            TotalForks = totalForks,
            TotalWatchers = totalWatchers,
            TotalOpenIssues = totalOpenIssues,
            TotalOpenPullRequests = openPRs,
            TotalClosedPullRequests = closedPRs,
            TotalInternalContributors = internalContributors,
            TotalExternalContributors = externalContributors,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<DependentRepositories> GetDependentRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dependencyAnalyzer.GetDependentRepositoriesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DetailedInsights> GetDetailedInsightsAsync(CancellationToken cancellationToken = default)
    {
        return await _healthAnalyzer.GetDetailedInsightsAsync(cancellationToken);
    }
}

