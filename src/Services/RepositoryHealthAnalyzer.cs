using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of repository health analyzer
/// </summary>
public class RepositoryHealthAnalyzer : IRepositoryHealthAnalyzer
{
    private readonly GitHubOptions _options;
    private readonly PerformanceOptions _performanceOptions;
    private readonly ICachingService _cachingService;
    private readonly IGitHubApiClient _apiClient;
    private readonly IRepositoryFetcher _repositoryFetcher;
    private readonly ILogger<RepositoryHealthAnalyzer> _logger;

    public RepositoryHealthAnalyzer(
        IOptions<GitHubOptions> options,
        IOptions<PerformanceOptions> performanceOptions,
        ICachingService cachingService,
        IGitHubApiClient apiClient,
        IRepositoryFetcher repositoryFetcher,
        ILogger<RepositoryHealthAnalyzer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _performanceOptions = performanceOptions?.Value ?? throw new ArgumentNullException(nameof(performanceOptions));
        _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repositoryFetcher = repositoryFetcher ?? throw new ArgumentNullException(nameof(repositoryFetcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DetailedInsights> GetDetailedInsightsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachingService.TryGetValue(CacheKeys.DetailedInsights, out DetailedInsights? cachedInsights))
        {
            _logger.LogInformation(
                "Returning cached detailed insights for organization {Organization}",
                _options.Organization);
            return cachedInsights!;
        }

        _logger.LogInformation(
            "Fetching detailed insights from GitHub API for organization {Organization}",
            _options.Organization);

        var client = _apiClient.CreateClient();

        try
        {
            var repos = await _repositoryFetcher.FetchAllRepositoriesAsync(client, cancellationToken);

            // Calculate activity score and get top 10 repositories
            var topRepos = repos
                .Select(r => new RepositoryInsight
                {
                    Name = r.Name,
                    Url = r.Html_Url,
                    Description = r.Description,
                    Language = r.Language,
                    Stars = r.Stargazers_Count,
                    Forks = r.Forks_Count,
                    OpenIssues = r.Open_Issues_Count,
                    UpdatedAt = r.Updated_At,
                    ActivityScore = CalculateActivityScore(r)
                })
                .OrderByDescending(r => r.ActivityScore)
                .Take(10)
                .ToList();

            // Calculate language distribution
            var languageGroups = repos
                .Where(r => !string.IsNullOrEmpty(r.Language))
                .GroupBy(r => r.Language!)
                .Select(g => new LanguageStats
                {
                    Language = g.Key,
                    RepositoryCount = g.Count(),
                    Percentage = Math.Round((double)g.Count() / repos.Count * 100, 2)
                })
                .OrderByDescending(l => l.RepositoryCount)
                .ToList();

            // Calculate activity breakdown
            var activeRepos = repos.Count(r => r.Updated_At.HasValue &&
                                               r.Updated_At.Value > DateTime.UtcNow.AddDays(-30));
            var archivedRepos = repos.Count(r => r.Archived);
            var totalStars = repos.Sum(r => r.Stargazers_Count);
            var totalForks = repos.Sum(r => r.Forks_Count);

            var activity = new ActivityBreakdown
            {
                TotalEngagement = totalStars + totalForks,
                ActiveRepositories = activeRepos,
                ArchivedRepositories = archivedRepos,
                AverageStarsPerRepo = repos.Count > 0 ? Math.Round((double)totalStars / repos.Count, 2) : 0,
                AverageForksPerRepo = repos.Count > 0 ? Math.Round((double)totalForks / repos.Count, 2) : 0
            };

            // Calculate repository health
            var health = CalculateRepositoryHealth(repos);

            var response = new DetailedInsights
            {
                Organization = _options.Organization,
                TopRepositories = topRepos,
                LanguageDistribution = languageGroups,
                Activity = activity,
                Health = health,
                Timestamp = DateTime.UtcNow
            };

            _cachingService.Set(CacheKeys.DetailedInsights, response);

            _logger.LogInformation(
                "Successfully fetched detailed insights: {TopRepos} top repos, {Languages} languages",
                topRepos.Count,
                languageGroups.Count);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error while fetching detailed insights for organization {Organization}",
                _options.Organization);
            throw new InvalidOperationException($"Failed to fetch detailed insights from GitHub: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while fetching detailed insights for organization {Organization}",
                _options.Organization);
            throw;
        }
    }

    /// <summary>
    /// Calculates an activity score based on stars, forks, and recent updates
    /// </summary>
    private int CalculateActivityScore(GitHubRepository repo)
    {
        var score = repo.Stargazers_Count * _performanceOptions.ActivityScoreStarsWeight + 
                    repo.Forks_Count * _performanceOptions.ActivityScoreForksWeight + 
                    repo.Open_Issues_Count * _performanceOptions.ActivityScoreIssuesWeight;

        // Bonus for recently updated repos
        if (repo.Updated_At.HasValue && repo.Updated_At.Value > DateTime.UtcNow.AddDays(-30))
        {
            score += _performanceOptions.ActivityScoreRecentUpdateBonus;
        }

        // Penalty for archived repos
        if (repo.Archived)
        {
            score /= 10;
        }

        return score;
    }

    /// <summary>
    /// Calculates repository health metrics based on update frequency and issue load
    /// </summary>
    private RepositoryHealth CalculateRepositoryHealth(List<GitHubRepository> repos)
    {
        var now = DateTime.UtcNow;
        var healthyRepos = new List<GitHubRepository>();
        var needsAttentionRepos = new List<GitHubRepository>();
        var atRiskRepos = new List<GitHubRepository>();
        var archivedRepos = repos.Where(r => r.Archived).ToList();

        foreach (var repo in repos.Where(r => !r.Archived))
        {
            var daysSinceUpdate = repo.Updated_At.HasValue
                ? (int)(now - repo.Updated_At.Value).TotalDays
                : int.MaxValue;

            // Calculate issue density (issues per star, with minimum threshold)
            var issueRatio = repo.Stargazers_Count > 0
                ? (double)repo.Open_Issues_Count / repo.Stargazers_Count
                : repo.Open_Issues_Count > 10 ? 1.0 : 0.0;

            if (daysSinceUpdate >= _performanceOptions.HealthCheckStaleDays)
            {
                atRiskRepos.Add(repo);
            }
            else if (daysSinceUpdate >= _performanceOptions.HealthCheckAttentionDays || issueRatio > 0.5 || repo.Open_Issues_Count > 50)
            {
                needsAttentionRepos.Add(repo);
            }
            else
            {
                healthyRepos.Add(repo);
            }
        }

        var totalActiveRepos = repos.Count(r => !r.Archived);
        var staleCount = atRiskRepos.Count;
        var stalePercentage = totalActiveRepos > 0
            ? Math.Round((double)staleCount / totalActiveRepos * 100, 1)
            : 0;

        // Get top repositories needing attention (by open issues)
        var topNeedsAttention = needsAttentionRepos
            .OrderByDescending(r => r.Open_Issues_Count)
            .Take(5)
            .Select(r => new RepositoryHealthDetail
            {
                Name = r.Name,
                Url = r.Html_Url,
                DaysSinceUpdate = r.Updated_At.HasValue ? (int)(now - r.Updated_At.Value).TotalDays : 999,
                OpenIssues = r.Open_Issues_Count,
                Reason = GetHealthReason(r, now)
            })
            .ToList();

        // Get top at-risk repositories (oldest first)
        var topAtRisk = atRiskRepos
            .OrderBy(r => r.Updated_At ?? DateTime.MinValue)
            .Take(5)
            .Select(r => new RepositoryHealthDetail
            {
                Name = r.Name,
                Url = r.Html_Url,
                DaysSinceUpdate = r.Updated_At.HasValue ? (int)(now - r.Updated_At.Value).TotalDays : 999,
                OpenIssues = r.Open_Issues_Count,
                Reason = GetHealthReason(r, now)
            })
            .ToList();

        return new RepositoryHealth
        {
            HealthyCount = healthyRepos.Count,
            NeedsAttentionCount = needsAttentionRepos.Count,
            AtRiskCount = atRiskRepos.Count,
            ArchivedCount = archivedRepos.Count,
            StalePercentage = stalePercentage,
            RepositoriesNeedingAttention = topNeedsAttention,
            AtRiskRepositories = topAtRisk
        };
    }

    /// <summary>
    /// Determines the reason for a repository's health concern
    /// </summary>
    private string GetHealthReason(GitHubRepository repo, DateTime now)
    {
        var daysSinceUpdate = repo.Updated_At.HasValue
            ? (int)(now - repo.Updated_At.Value).TotalDays
            : 999;

        var reasons = new List<string>();

        if (daysSinceUpdate >= 365)
            reasons.Add($"No updates in {daysSinceUpdate / 365}+ years");
        else if (daysSinceUpdate >= 180)
            reasons.Add($"No updates in {daysSinceUpdate} days");
        else if (daysSinceUpdate >= 30)
            reasons.Add($"Not updated in {daysSinceUpdate} days");

        if (repo.Open_Issues_Count > 50)
            reasons.Add($"{repo.Open_Issues_Count} open issues");
        else if (repo.Open_Issues_Count > 20)
            reasons.Add($"{repo.Open_Issues_Count} issues need triage");

        var issueRatio = repo.Stargazers_Count > 0
            ? (double)repo.Open_Issues_Count / repo.Stargazers_Count
            : 0;

        if (issueRatio > 0.5 && repo.Open_Issues_Count > 10)
            reasons.Add("High issue-to-star ratio");

        return reasons.Count > 0 ? string.Join(", ", reasons) : "Needs review";
    }
}
