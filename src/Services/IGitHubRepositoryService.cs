using GitHubInsights.Models;

namespace GitHubInsights.Services;

/// <summary>
/// Service interface for GitHub repository operations
/// </summary>
public interface IGitHubRepositoryService
{
    /// <summary>
    /// Gets the total number of repositories for the configured organization
    /// </summary>
    /// <returns>GitHub insights response with repository count</returns>
    Task<GitHubInsightsResponse> GetRepositoryCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed repository statistics including stars, forks, and more
    /// </summary>
    /// <returns>Detailed repository statistics</returns>
    Task<RepositoryDetailsResponse> GetRepositoryDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets basic repository statistics (fast) without contributor analysis
    /// </summary>
    /// <returns>Basic repository statistics</returns>
    Task<RepositoryDetailsResponse> GetBasicRepositoryDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about repositories that depend on this organization's code
    /// </summary>
    /// <returns>Dependent repositories information</returns>
    Task<DependentRepositories> GetDependentRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed insights including top repositories, language distribution, and activity breakdown
    /// </summary>
    /// <returns>Detailed insights with comprehensive statistics</returns>
    Task<DetailedInsights> GetDetailedInsightsAsync(CancellationToken cancellationToken = default);
}
