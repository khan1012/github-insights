namespace GitHubInsights.Models;

/// <summary>
/// Represents contributor statistics for the organization
/// </summary>
public class ContributorStats
{
    /// <summary>
    /// Total number of internal contributors (organization members)
    /// </summary>
    public int TotalInternalContributors { get; set; }

    /// <summary>
    /// Total number of external contributors (non-members)
    /// </summary>
    public int TotalExternalContributors { get; set; }

    /// <summary>
    /// Timestamp when this data was retrieved
    /// </summary>
    public DateTime Timestamp { get; set; }
}
