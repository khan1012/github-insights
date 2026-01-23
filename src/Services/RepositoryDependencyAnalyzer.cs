using GitHubInsights.Configuration;
using GitHubInsights.Constants;
using GitHubInsights.Models;
using GitHubInsights.Models.GitHub;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of repository dependency analyzer
/// </summary>
public class RepositoryDependencyAnalyzer : IRepositoryDependencyAnalyzer
{
    private readonly GitHubOptions _options;
    private readonly ICachingService _cachingService;
    private readonly IGitHubApiClient _apiClient;
    private readonly IRepositoryFetcher _repositoryFetcher;
    private readonly ILogger<RepositoryDependencyAnalyzer> _logger;

    public RepositoryDependencyAnalyzer(
        IOptions<GitHubOptions> options,
        ICachingService cachingService,
        IGitHubApiClient apiClient,
        IRepositoryFetcher repositoryFetcher,
        ILogger<RepositoryDependencyAnalyzer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repositoryFetcher = repositoryFetcher ?? throw new ArgumentNullException(nameof(repositoryFetcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DependentRepositories> GetDependentRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachingService.TryGetValue(CacheKeys.DependentRepositories, out DependentRepositories? cachedData))
        {
            _logger.LogInformation(
                "Returning cached dependent repositories data for organization {Organization}",
                _options.Organization);
            return cachedData!;
        }

        _logger.LogInformation(
            "Fetching dependent repositories from GitHub API for organization {Organization}",
            _options.Organization);

        using var client = _apiClient.CreateClient();

        try
        {
            // Fetch all repositories
            var repos = await _repositoryFetcher.FetchAllRepositoriesAsync(client, cancellationToken);

            var topRepositories = new List<RepositoryDependencyInfo>();
            int totalDependents = 0;
            int packageRepos = 0;

            // Prioritize repos most likely to be packages:
            // 1. Sort by stars (popular repos more likely to be packages)
            // 2. Filter out docs/website repos
            // 3. Take top N to get better sample (configurable)
            var limit = _options.MaxRepositoriesForContributorAnalysis;
            var candidateRepos = repos
                .Where(r => !r.Name.ToLowerInvariant().Contains("website") &&
                           !r.Name.ToLowerInvariant().Contains("docs") &&
                           !r.Name.ToLowerInvariant().Contains("example") &&
                           !r.Name.ToLowerInvariant().Contains("demo") &&
                           !r.Name.ToLowerInvariant().Contains("tutorial"))
                .OrderByDescending(r => r.Stargazers_Count)
                .Take(limit)
                .ToList();

            _logger.LogInformation(
                "Analyzing {Count} repositories (from {Total} total) for dependency information",
                candidateRepos.Count,
                repos.Count);

            // For each repository, try to get dependency information
            foreach (var repo in candidateRepos)
            {
                var dependentCount = await FetchDependentCountAsync(client, repo.Name, cancellationToken);

                if (dependentCount > 0)
                {
                    packageRepos++;
                    totalDependents += dependentCount;

                    topRepositories.Add(new RepositoryDependencyInfo
                    {
                        Name = repo.Name,
                        DependentCount = dependentCount,
                        PackageName = repo.Name,
                        Ecosystem = DetermineEcosystem(repo)
                    });
                }
            }

            // Sort by dependent count and take top 10
            topRepositories = topRepositories
                .OrderByDescending(r => r.DependentCount)
                .Take(10)
                .ToList();

            var response = new DependentRepositories
            {
                Organization = _options.Organization,
                TotalDependents = totalDependents,
                RepositoriesAnalyzed = candidateRepos.Count,
                PackageRepositories = packageRepos,
                TopRepositories = topRepositories,
                Timestamp = DateTime.UtcNow
            };

            _cachingService.Set(CacheKeys.DependentRepositories, response);

            _logger.LogInformation(
                "Successfully fetched dependent repositories: {Total} dependents across {PackageCount} packages",
                totalDependents,
                packageRepos);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error while fetching dependent repositories for organization {Organization}",
                _options.Organization);
            throw new InvalidOperationException($"Failed to fetch dependent repositories from GitHub: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while fetching dependent repositories for organization {Organization}",
                _options.Organization);
            throw;
        }
    }

    private async Task<int> FetchDependentCountAsync(HttpClient client, string repoName, CancellationToken cancellationToken)
    {
        try
        {
            // Use GitHub's community API endpoint to get dependent count
            // This is displayed as "Used by X" on GitHub repo pages
            var url = $"https://github.com/{_options.Organization}/{repoName}/network/dependents";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse the HTML to find the dependent count
            // Look for pattern like "Used by 123" or dependency count in the page
            var match = System.Text.RegularExpressions.Regex.Match(
                content,
                @"(\d+(?:,\d+)*)\s+Repositor(?:y|ies)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var countStr = match.Groups[1].Value.Replace(",", "");
                if (int.TryParse(countStr, out var count))
                {
                    return count;
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch dependent count for repository {RepoName}",
                repoName);
            return 0;
        }
    }

    private string? DetermineEcosystem(GitHubRepository repo)
    {
        // Try to determine the package ecosystem based on repo name patterns
        var name = repo.Name.ToLowerInvariant();

        if (name.Contains("npm") || name.Contains("node") || name.Contains("js") || name.Contains("typescript"))
            return "npm";

        if (name.Contains("maven") || name.Contains("java"))
            return "maven";

        if (name.Contains("nuget") || name.Contains("dotnet") || name.Contains("csharp"))
            return "nuget";

        if (name.Contains("python") || name.Contains("py"))
            return "pypi";

        if (name.Contains("go") || name.Contains("golang"))
            return "go";

        if (name.Contains("rust") || name.Contains("cargo"))
            return "cargo";

        return null;
    }
}
