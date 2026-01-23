using System.Text.Json;
using FluentAssertions;
using GitHubInsights.Models.GitHub;

namespace GitHubInsights.Tests.Models;

/// <summary>
/// Tests for GitHubSearchResponse DTO
/// </summary>
public class GitHubSearchResponseTests
{
    [Fact]
    public void GitHubSearchResponse_ShouldDeserializeFromGitHubJson()
    {
        // Arrange - Actual GitHub Search API response format
        var json = """
        {
            "total_count": 416,
            "incomplete_results": false,
            "items": []
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<GitHubSearchResponse>(json);

        // Assert
        response.Should().NotBeNull();
        response!.Total_Count.Should().Be(416);
    }

    [Fact]
    public void GitHubSearchResponse_ShouldHandleZeroCount()
    {
        // Arrange
        var json = """
        {
            "total_count": 0,
            "incomplete_results": false,
            "items": []
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<GitHubSearchResponse>(json);

        // Assert
        response.Should().NotBeNull();
        response!.Total_Count.Should().Be(0);
    }

    [Fact]
    public void GitHubSearchResponse_ShouldHandleLargeCount()
    {
        // Arrange
        var json = """
        {
            "total_count": 13006,
            "incomplete_results": false,
            "items": []
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<GitHubSearchResponse>(json);

        // Assert
        response.Should().NotBeNull();
        response!.Total_Count.Should().Be(13006);
    }
}
