using FluentAssertions;
using GitHubInsights.Constants;

namespace GitHubInsights.Tests.Constants;

/// <summary>
/// Tests for CacheKeys constants
/// </summary>
public class CacheKeysTests
{
    [Fact]
    public void RepositoryCount_ShouldHaveCorrectValue()
    {
        // Assert
        CacheKeys.RepositoryCount.Should().Be("GitHubRepoCount");
    }

    [Fact]
    public void RepositoryDetails_ShouldHaveCorrectValue()
    {
        // Assert
        CacheKeys.RepositoryDetails.Should().Be("GitHubRepoDetails");
    }

    [Fact]
    public void CacheKeys_ShouldBeUnique()
    {
        // Assert
        CacheKeys.RepositoryCount.Should().NotBe(CacheKeys.RepositoryDetails);
    }
}
