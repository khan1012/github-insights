using FluentAssertions;
using GitHubInsights.Constants;

namespace GitHubInsights.Tests.Constants;

/// <summary>
/// Tests for GitHubApiEndpoints to ensure URLs are constructed correctly
/// </summary>
public class GitHubApiEndpointsTests
{
    [Fact]
    public void GetOrgRepositories_ShouldConstructCorrectUrl()
    {
        // Act
        var url = GitHubApiEndpoints.GetOrgRepositories("test-org", 1, 100);

        // Assert
        url.Should().Be("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated");
    }

    [Fact]
    public void GetOrgRepositories_ShouldHandleDifferentPageNumbers()
    {
        // Act
        var url = GitHubApiEndpoints.GetOrgRepositories("test-org", 5, 50);

        // Assert
        url.Should().Be("https://api.github.com/orgs/test-org/repos?page=5&per_page=50&sort=updated");
    }

    [Fact]
    public void GetOrgRepositoriesForCount_ShouldConstructCorrectUrl()
    {
        // Act
        var url = GitHubApiEndpoints.GetOrgRepositoriesForCount("test-org");

        // Assert
        url.Should().Be("https://api.github.com/orgs/test-org/repos?per_page=1");
    }

    [Fact]
    public void GetOrganization_ShouldConstructCorrectUrl()
    {
        // Act
        var url = GitHubApiEndpoints.GetOrganization("test-org");

        // Assert
        url.Should().Be("https://api.github.com/orgs/test-org");
    }

    [Fact]
    public void SearchIssues_ShouldConstructCorrectUrl()
    {
        // Arrange
        var query = "type:pr+state:open+org:test-org";

        // Act
        var url = GitHubApiEndpoints.SearchIssues(query);

        // Assert
        url.Should().Be("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1");
    }

    [Fact]
    public void BuildPullRequestQuery_ShouldConstructOpenPRQuery()
    {
        // Act
        var query = GitHubApiEndpoints.BuildPullRequestQuery("test-org", "open");

        // Assert
        query.Should().Be("type:pr+state:open+org:test-org");
    }

    [Fact]
    public void BuildPullRequestQuery_ShouldConstructClosedPRQuery()
    {
        // Act
        var query = GitHubApiEndpoints.BuildPullRequestQuery("test-org", "closed");

        // Assert
        query.Should().Be("type:pr+state:closed+org:test-org");
    }

    [Fact]
    public void GetOpenPRsQuery_ShouldConstructCorrectQuery()
    {
        // Act
        var query = GitHubApiEndpoints.GetOpenPRsQuery("test-org");

        // Assert
        query.Should().Be("type:pr+state:open+org:test-org");
    }

    [Fact]
    public void GetClosedPRsQuery_ShouldConstructCorrectQuery()
    {
        // Act
        var query = GitHubApiEndpoints.GetClosedPRsQuery("test-org");

        // Assert
        query.Should().Be("type:pr+state:closed+org:test-org");
    }
}
