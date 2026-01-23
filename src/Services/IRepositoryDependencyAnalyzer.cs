using GitHubInsights.Models;

namespace GitHubInsights.Services;

/// <summary>
/// Analyzer for repository dependencies and package usage
/// </summary>
public interface IRepositoryDependencyAnalyzer
{
    /// <summary>
    /// Analyze dependent repositories (who uses our packages)
    /// </summary>
    Task<DependentRepositories> GetDependentRepositoriesAsync(CancellationToken cancellationToken = default);
}
