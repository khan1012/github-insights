using System.Text.Json.Serialization;

namespace GitHubInsights.Models.GitHub;

/// <summary>
/// Represents a GitHub repository contributor
/// </summary>
public class GitHubContributor
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("contributions")]
    public int Contributions { get; set; }
}
