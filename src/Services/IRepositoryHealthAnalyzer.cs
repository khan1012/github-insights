using GitHubInsights.Models;

namespace GitHubInsights.Services;

/// <summary>
/// Analyzer for repository health and detailed insights
/// </summary>
public interface IRepositoryHealthAnalyzer
{
    /// <summary>
    /// Get detailed insights including activity scores, language distribution, and health metrics
    /// </summary>
    Task<DetailedInsights> GetDetailedInsightsAsync(CancellationToken cancellationToken = default);
}
