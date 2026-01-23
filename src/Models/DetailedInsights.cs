namespace GitHubInsights.Models;

/// <summary>
/// Detailed insights response with top repositories and activity breakdown
/// </summary>
public class DetailedInsights
{
    /// <summary>
    /// Organization name
    /// </summary>
    public required string Organization { get; init; }

    /// <summary>
    /// Top repositories by stars
    /// </summary>
    public List<RepositoryInsight> TopRepositories { get; init; } = new();

    /// <summary>
    /// Language distribution across repositories
    /// </summary>
    public List<LanguageStats> LanguageDistribution { get; init; } = new();

    /// <summary>
    /// Activity breakdown by category
    /// </summary>
    public ActivityBreakdown Activity { get; init; } = new();

    /// <summary>
    /// Repository health overview
    /// </summary>
    public RepositoryHealth Health { get; init; } = new();

    /// <summary>
    /// Timestamp when the data was retrieved
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Individual repository insight details
/// </summary>
public class RepositoryInsight
{
    /// <summary>
    /// Repository name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Repository URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Primary programming language
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Number of stars
    /// </summary>
    public int Stars { get; init; }

    /// <summary>
    /// Number of forks
    /// </summary>
    public int Forks { get; init; }

    /// <summary>
    /// Number of open issues
    /// </summary>
    public int OpenIssues { get; init; }

    /// <summary>
    /// When the repository was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Activity score (calculated from stars, forks, and recent activity)
    /// </summary>
    public int ActivityScore { get; init; }
}

/// <summary>
/// Language distribution statistics
/// </summary>
public class LanguageStats
{
    /// <summary>
    /// Programming language name
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Number of repositories using this language
    /// </summary>
    public int RepositoryCount { get; init; }

    /// <summary>
    /// Percentage of total repositories
    /// </summary>
    public double Percentage { get; init; }
}

/// <summary>
/// Activity breakdown across different metrics
/// </summary>
public class ActivityBreakdown
{
    /// <summary>
    /// Total engagement (stars + forks)
    /// </summary>
    public int TotalEngagement { get; init; }

    /// <summary>
    /// Active repositories (updated in last 30 days)
    /// </summary>
    public int ActiveRepositories { get; init; }

    /// <summary>
    /// Archived repositories
    /// </summary>
    public int ArchivedRepositories { get; init; }

    /// <summary>
    /// Average stars per repository
    /// </summary>
    public double AverageStarsPerRepo { get; init; }

    /// <summary>
    /// Average forks per repository
    /// </summary>
    public double AverageForksPerRepo { get; init; }
}

/// <summary>
/// Repository health overview metrics
/// </summary>
public class RepositoryHealth
{
    /// <summary>
    /// Healthy repositories (updated in last 30 days, reasonable issue load)
    /// </summary>
    public int HealthyCount { get; init; }

    /// <summary>
    /// Repositories needing attention (30-180 days old or high issue ratio)
    /// </summary>
    public int NeedsAttentionCount { get; init; }

    /// <summary>
    /// Stale or at-risk repositories (180+ days old)
    /// </summary>
    public int AtRiskCount { get; init; }

    /// <summary>
    /// Archived repositories
    /// </summary>
    public int ArchivedCount { get; init; }

    /// <summary>
    /// Percentage of stale repositories
    /// </summary>
    public double StalePercentage { get; init; }

    /// <summary>
    /// List of repositories needing attention
    /// </summary>
    public List<RepositoryHealthDetail> RepositoriesNeedingAttention { get; init; } = new();

    /// <summary>
    /// List of at-risk repositories
    /// </summary>
    public List<RepositoryHealthDetail> AtRiskRepositories { get; init; } = new();
}

/// <summary>
/// Individual repository health details
/// </summary>
public class RepositoryHealthDetail
{
    /// <summary>
    /// Repository name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Repository URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Days since last update
    /// </summary>
    public int DaysSinceUpdate { get; init; }

    /// <summary>
    /// Number of open issues
    /// </summary>
    public int OpenIssues { get; init; }

    /// <summary>
    /// Reason for health concern
    /// </summary>
    public required string Reason { get; init; }
}
