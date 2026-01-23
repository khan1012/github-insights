using GitHubInsights.Models;

namespace GitHubInsights.Services;

/// <summary>
/// Analyzer for determining top contributors
/// </summary>
public interface ITopContributorsAnalyzer
{
    /// <summary>
    /// Analyze and get top contributors for the organization
    /// </summary>
    Task<TopContributors> GetTopContributorsAsync(CancellationToken cancellationToken = default);
}
