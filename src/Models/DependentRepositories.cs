namespace GitHubInsights.Models;

/// <summary>
/// Represents information about repositories that depend on the organization's code
/// </summary>
public class DependentRepositories
{
    /// <summary>
    /// Organization name
    /// </summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Total number of public repositories that depend on this organization's packages
    /// </summary>
    public int TotalDependents { get; set; }

    /// <summary>
    /// Number of repositories analyzed for dependency information
    /// </summary>
    public int RepositoriesAnalyzed { get; set; }

    /// <summary>
    /// Number of repositories that publish packages (have dependents)
    /// </summary>
    public int PackageRepositories { get; set; }

    /// <summary>
    /// Top repositories with the most dependents
    /// </summary>
    public List<RepositoryDependencyInfo> TopRepositories { get; set; } = new();

    /// <summary>
    /// When this data was fetched
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Information about a single repository's dependents
/// </summary>
public class RepositoryDependencyInfo
{
    /// <summary>
    /// Repository name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of repositories depending on this one
    /// </summary>
    public int DependentCount { get; set; }

    /// <summary>
    /// Package name (if published as a package)
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Package ecosystem (npm, maven, nuget, etc.)
    /// </summary>
    public string? Ecosystem { get; set; }
}
