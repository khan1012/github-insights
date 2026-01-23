using System.Text.Json.Serialization;

namespace GitHubInsights.Models.GitHub;

/// <summary>
/// DTO for GitHub search API response
/// </summary>
public class GitHubSearchResponse
{
    [JsonPropertyName("total_count")]
    public int Total_Count { get; set; }
}
