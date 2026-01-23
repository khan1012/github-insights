using System.Text.Json.Serialization;

namespace GitHubInsights.Models.GitHub;

/// <summary>
/// Represents a GitHub user profile
/// </summary>
public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("followers")]
    public int Followers { get; set; }

    [JsonPropertyName("following")]
    public int Following { get; set; }

    [JsonPropertyName("public_repos")]
    public int Public_Repos { get; set; }
}
