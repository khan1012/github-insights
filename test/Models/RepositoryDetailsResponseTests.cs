using FluentAssertions;
using GitHubInsights.Models;
using System.Text.Json;

namespace GitHubInsights.Tests.Models;

/// <summary>
/// Tests for RepositoryDetailsResponse model
/// </summary>
public class RepositoryDetailsResponseTests
{
    [Fact]
    public void RepositoryDetailsResponse_ShouldSerializeCorrectly()
    {
        // Arrange
        var response = new RepositoryDetailsResponse
        {
            Organization = "test-org",
            TotalRepositories = 100,
            TotalStars = 5000,
            TotalForks = 2000,
            TotalWatchers = 1500,
            TotalOpenIssues = 50,
            TotalOpenPullRequests = 25,
            TotalClosedPullRequests = 500,
            TotalInternalContributors = 15,
            TotalExternalContributors = 35,
            Timestamp = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<RepositoryDetailsResponse>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Organization.Should().Be("test-org");
        deserialized.TotalRepositories.Should().Be(100);
        deserialized.TotalStars.Should().Be(5000);
        deserialized.TotalInternalContributors.Should().Be(15);
        deserialized.TotalExternalContributors.Should().Be(35);
    }

    [Fact]
    public void RepositoryDetailsResponse_ShouldCalculateTotalContributors()
    {
        // Arrange
        var response = new RepositoryDetailsResponse
        {
            Organization = "test-org",
            TotalRepositories = 50,
            TotalStars = 1000,
            TotalForks = 500,
            TotalWatchers = 300,
            TotalOpenIssues = 10,
            TotalOpenPullRequests = 5,
            TotalClosedPullRequests = 100,
            TotalInternalContributors = 20,
            TotalExternalContributors = 80,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var totalContributors = response.TotalInternalContributors + response.TotalExternalContributors;

        // Assert
        totalContributors.Should().Be(100);
        response.TotalInternalContributors.Should().BeLessThan(totalContributors);
        response.TotalExternalContributors.Should().BeLessThan(totalContributors);
    }

    [Fact]
    public void RepositoryDetailsResponse_ShouldHaveConsistentData()
    {
        // Arrange & Act
        var response = new RepositoryDetailsResponse
        {
            Organization = "test-org",
            TotalRepositories = 10,
            TotalStars = 100,
            TotalForks = 50,
            TotalWatchers = 75,
            TotalOpenIssues = 20,
            TotalOpenPullRequests = 10,
            TotalClosedPullRequests = 100,
            TotalInternalContributors = 5,
            TotalExternalContributors = 15,
            Timestamp = DateTime.UtcNow
        };

        // Assert - verify data consistency
        response.TotalRepositories.Should().BeGreaterThan(0);
        response.TotalStars.Should().BeGreaterThanOrEqualTo(0);
        response.TotalForks.Should().BeGreaterThanOrEqualTo(0);
        response.TotalInternalContributors.Should().BeGreaterThanOrEqualTo(0);
        response.TotalExternalContributors.Should().BeGreaterThanOrEqualTo(0);
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}
