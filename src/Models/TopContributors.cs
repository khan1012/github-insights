namespace GitHubInsights.Models;

/// <summary>
/// Response containing top contributors information
/// </summary>
public class TopContributors
{
    /// <summary>
    /// Organization name
    /// </summary>
    public required string Organization { get; init; }

    /// <summary>
    /// List of top contributors
    /// </summary>
    public List<ContributorDetail> Contributors { get; init; } = new();

    /// <summary>
    /// Timestamp when data was retrieved
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Individual contributor details
/// </summary>
public class ContributorDetail
{
    /// <summary>
    /// GitHub username
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Profile URL
    /// </summary>
    public required string ProfileUrl { get; init; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Total contributions across all repositories
    /// </summary>
    public int TotalContributions { get; init; }

    /// <summary>
    /// Number of repositories contributed to
    /// </summary>
    public int RepositoriesContributedTo { get; init; }

    /// <summary>
    /// Number of followers
    /// </summary>
    public int Followers { get; init; }

    /// <summary>
    /// Whether this is an internal contributor (organization member)
    /// </summary>
    public bool IsInternal { get; init; }
}
