using System.Text.Json;
using FluentAssertions;
using GitHubInsights.Models.GitHub;

namespace GitHubInsights.Tests.Models;

/// <summary>
/// Tests for GitHubRepository DTO to ensure JSON deserialization works correctly
/// </summary>
public class GitHubRepositoryTests
{
    [Fact]
    public void GitHubRepository_ShouldDeserializeFromGitHubJson_WithCorrectPropertyMapping()
    {
        // Arrange - Actual GitHub API response format
        var json = """
        {
            "name": "test-repo",
            "html_url": "https://github.com/org/test-repo",
            "description": "Test repository",
            "stargazers_count": 100,
            "forks_count": 50,
            "watchers_count": 75,
            "open_issues_count": 10
        }
        """;

        // Act
        var repo = JsonSerializer.Deserialize<GitHubRepository>(json);

        // Assert
        repo.Should().NotBeNull();
        repo!.Name.Should().Be("test-repo");
        repo.Html_Url.Should().Be("https://github.com/org/test-repo");
        repo.Description.Should().Be("Test repository");
        repo.Stargazers_Count.Should().Be(100);
        repo.Forks_Count.Should().Be(50);
        repo.Watchers_Count.Should().Be(75);
        repo.Open_Issues_Count.Should().Be(10);
    }

    [Fact]
    public void GitHubRepository_ShouldHandleNullDescription()
    {
        // Arrange
        var json = """
        {
            "name": "test-repo",
            "html_url": "https://github.com/org/test-repo",
            "description": null,
            "stargazers_count": 100,
            "forks_count": 50,
            "watchers_count": 75,
            "open_issues_count": 10
        }
        """;

        // Act
        var repo = JsonSerializer.Deserialize<GitHubRepository>(json);

        // Assert
        repo.Should().NotBeNull();
        repo!.Description.Should().BeNull();
    }

    [Fact]
    public void GitHubRepository_ShouldDeserializeArray()
    {
        // Arrange
        var json = """
        [
            {
                "name": "repo1",
                "html_url": "https://github.com/org/repo1",
                "description": "First repo",
                "stargazers_count": 100,
                "forks_count": 50,
                "watchers_count": 75,
                "open_issues_count": 10
            },
            {
                "name": "repo2",
                "html_url": "https://github.com/org/repo2",
                "description": "Second repo",
                "stargazers_count": 200,
                "forks_count": 100,
                "watchers_count": 150,
                "open_issues_count": 20
            }
        ]
        """;

        // Act
        var repos = JsonSerializer.Deserialize<GitHubRepository[]>(json);

        // Assert
        repos.Should().NotBeNull();
        repos.Should().HaveCount(2);
        repos![0].Stargazers_Count.Should().Be(100);
        repos[1].Stargazers_Count.Should().Be(200);
    }

    [Fact]
    public void GitHubRepository_ShouldAggregateCorrectly()
    {
        // Arrange
        var repos = new List<GitHubRepository>
        {
            new() { Name = "repo1", Stargazers_Count = 100, Forks_Count = 50, Watchers_Count = 75, Open_Issues_Count = 10 },
            new() { Name = "repo2", Stargazers_Count = 200, Forks_Count = 100, Watchers_Count = 150, Open_Issues_Count = 20 },
            new() { Name = "repo3", Stargazers_Count = 50, Forks_Count = 25, Watchers_Count = 40, Open_Issues_Count = 5 }
        };

        // Act
        var totalStars = repos.Sum(r => r.Stargazers_Count);
        var totalForks = repos.Sum(r => r.Forks_Count);
        var totalWatchers = repos.Sum(r => r.Watchers_Count);
        var totalIssues = repos.Sum(r => r.Open_Issues_Count);

        // Assert - This test would have caught the deserialization bug!
        totalStars.Should().Be(350);
        totalForks.Should().Be(175);
        totalWatchers.Should().Be(265);
        totalIssues.Should().Be(35);
    }
}
