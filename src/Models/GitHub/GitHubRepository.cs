using System.Text.Json.Serialization;

namespace GitHubInsights.Models.GitHub;

/// <summary>
/// DTO for GitHub repository response
/// </summary>
public class GitHubRepository
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string Html_Url { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("stargazers_count")]
    public int Stargazers_Count { get; set; }

    [JsonPropertyName("forks_count")]
    public int Forks_Count { get; set; }

    [JsonPropertyName("watchers_count")]
    public int Watchers_Count { get; set; }

    [JsonPropertyName("open_issues_count")]
    public int Open_Issues_Count { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? Updated_At { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }
}
