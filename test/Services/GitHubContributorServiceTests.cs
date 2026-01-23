using FluentAssertions;
using GitHubInsights.Configuration;
using GitHubInsights.Helpers;
using GitHubInsights.Models;
using GitHubInsights.Models.GitHub;
using GitHubInsights.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace GitHubInsights.Tests.Services;

/// <summary>
/// Tests for GitHubContributorService to ensure proper contributor operations
/// Tests run end-to-end with real service implementations, mocking only the HTTP layer
/// </summary>
public class GitHubContributorServiceTests : GitHubServiceTestBase
{
    private readonly Mock<ILogger<GitHubContributorService>> _mockLogger;

    public GitHubContributorServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitHubContributorService>>();
    }

    [Fact]
    public async Task GetContributorStatsAsync_ShouldReturnContributorCounts()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 100 },
            new { login = "external1", contributions = 50 }
        });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetContributorStatsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInternalContributors.Should().Be(1);
        result.TotalExternalContributors.Should().Be(1);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetContributorStatsAsync_ShouldCacheResults()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 100 },
            new { login = "external1", contributions = 50 }
        });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result1 = await service.GetContributorStatsAsync(CancellationToken.None);
        var result2 = await service.GetContributorStatsAsync(CancellationToken.None); // Should use cache

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.TotalInternalContributors.Should().Be(result2.TotalInternalContributors);
        result1.TotalExternalContributors.Should().Be(result2.TotalExternalContributors);
        mockHandler.RequestCount.Should().BeLessThan(15); // Second call should not make additional API calls
    }

    [Fact]
    public async Task GetContributorStatsAsync_ShouldHandleNoContributors()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new object[] { });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetContributorStatsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInternalContributors.Should().Be(0);
        result.TotalExternalContributors.Should().Be(0);
    }

    [Fact]
    public async Task GetFollowerReachAsync_ShouldReturnFollowerCounts()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 100 },
            new { login = "external1", contributions = 50 }
        });

        mockHandler.AddJsonResponse("https://api.github.com/users/member1", new
        {
            login = "member1",
            id = 1,
            followers = 250,
            following = 100,
            public_repos = 50
        });

        mockHandler.AddJsonResponse("https://api.github.com/users/external1", new
        {
            login = "external1",
            id = 2,
            followers = 1500,
            following = 200,
            public_repos = 75
        });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetFollowerReachAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalFollowers.Should().Be(1750); // 250 + 1500
        result.ContributorsAnalyzed.Should().Be(2);
        result.ContributorsFailed.Should().Be(0);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetFollowerReachAsync_ShouldCacheResults()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 100 }
        });

        mockHandler.AddJsonResponse("https://api.github.com/users/member1", new
        {
            login = "member1",
            id = 1,
            followers = 250,
            following = 100,
            public_repos = 50
        });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result1 = await service.GetFollowerReachAsync(CancellationToken.None);
        var result2 = await service.GetFollowerReachAsync(CancellationToken.None); // Should use cache

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.TotalFollowers.Should().Be(result2.TotalFollowers);
        result1.ContributorsAnalyzed.Should().Be(result2.ContributorsAnalyzed);
        mockHandler.RequestCount.Should().BeLessThan(10); // Second call should not make additional API calls
    }

    [Fact]
    public async Task GetFollowerReachAsync_ShouldHandleUserFetchFailures()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new object[] { });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 100 },
            new { login = "deleted-user", contributions = 50 }
        });

        mockHandler.AddJsonResponse("https://api.github.com/users/member1", new
        {
            login = "member1",
            id = 1,
            followers = 250,
            following = 100,
            public_repos = 50
        });

        // Simulate 404 for deleted user
        mockHandler.AddErrorResponse("https://api.github.com/users/deleted-user", System.Net.HttpStatusCode.NotFound);

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetFollowerReachAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalFollowers.Should().Be(250); // Only successful user counted
        result.ContributorsAnalyzed.Should().Be(1); // Only 1 successful
        result.ContributorsFailed.Should().Be(1); // 1 failed
    }

    [Fact]
    public async Task GetTopContributorsAsync_ShouldReturnTopContributors()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=1&per_page=100",
            new[] { new { login = "internal-user1" }, new { login = "internal-user2" } });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=2&per_page=100",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated",
            new[]
            {
                new { name = "repo1", stargazers_count = 1000, forks_count = 500, watchers_count = 100, open_issues_count = 10 },
                new { name = "repo2", stargazers_count = 800, forks_count = 300, watchers_count = 80, open_issues_count = 5 }
            });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/repos/test-org/repo1/contributors?per_page=100",
            new[]
            {
                new { login = "internal-user1", contributions = 150 },
                new { login = "external-user1", contributions = 100 },
                new { login = "internal-user2", contributions = 75 }
            });

        mockHandler.AddJsonResponse(
            "https://api.github.com/repos/test-org/repo2/contributors?per_page=100",
            new[]
            {
                new { login = "internal-user1", contributions = 50 },
                new { login = "external-user1", contributions = 80 }
            });

        mockHandler.AddJsonResponse(
            "https://api.github.com/users/internal-user1",
            new { login = "internal-user1", html_url = "https://github.com/internal-user1", avatar_url = "https://avatars.githubusercontent.com/u/1", followers = 150 });
        mockHandler.AddJsonResponse(
            "https://api.github.com/users/external-user1",
            new { login = "external-user1", html_url = "https://github.com/external-user1", avatar_url = "https://avatars.githubusercontent.com/u/2", followers = 200 });
        mockHandler.AddJsonResponse(
            "https://api.github.com/users/internal-user2",
            new { login = "internal-user2", html_url = "https://github.com/internal-user2", avatar_url = "https://avatars.githubusercontent.com/u/3", followers = 100 });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.github.com") };
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var service = CreateService();

        // Act
        var result = await service.GetTopContributorsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Organization.Should().Be("test-org");
        result.Contributors.Should().NotBeNull();
        result.Contributors.Should().HaveCountGreaterThan(0);

        var topContributor = result.Contributors.First();
        topContributor.Username.Should().Be("internal-user1");
        topContributor.TotalContributions.Should().Be(200);
        topContributor.RepositoriesContributedTo.Should().Be(2);
        topContributor.IsInternal.Should().BeTrue();
        topContributor.Followers.Should().Be(150);
        topContributor.AvatarUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTopContributorsAsync_ShouldCacheResults()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=1&per_page=100",
            new[] { new { login = "user1" } });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=2&per_page=100",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated",
            new[] { new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 } });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/repos/test-org/repo1/contributors?per_page=100",
            new[] { new { login = "user1", contributions = 100 } });

        mockHandler.AddJsonResponse(
            "https://api.github.com/users/user1",
            new { login = "user1", html_url = "https://github.com/user1", avatar_url = "https://avatars.githubusercontent.com/u/1", followers = 50 });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.github.com") };
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var service = CreateService();

        // Act
        var result1 = await service.GetTopContributorsAsync();
        var result2 = await service.GetTopContributorsAsync();

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Timestamp.Should().Be(result2.Timestamp);
        mockHandler.RequestCount.Should().BeLessThan(15); // Should not call API second time due to caching
    }

    [Fact]
    public async Task GetTopContributorsAsync_ShouldDistinguishInternalExternalContributors()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=1&per_page=100",
            new[] { new { login = "internal-user" } });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=2&per_page=100",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated",
            new[] { new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 } });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/repos/test-org/repo1/contributors?per_page=100",
            new[]
            {
                new { login = "external-user", contributions = 200 },
                new { login = "internal-user", contributions = 100 }
            });

        mockHandler.AddJsonResponse(
            "https://api.github.com/users/external-user",
            new { login = "external-user", html_url = "https://github.com/external-user", avatar_url = "https://avatars.githubusercontent.com/u/1", followers = 300 });
        mockHandler.AddJsonResponse(
            "https://api.github.com/users/internal-user",
            new { login = "internal-user", html_url = "https://github.com/internal-user", avatar_url = "https://avatars.githubusercontent.com/u/2", followers = 150 });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.github.com") };
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var service = CreateService();

        // Act
        var result = await service.GetTopContributorsAsync();

        // Assert
        var externalContributor = result.Contributors.First(c => c.Username == "external-user");
        externalContributor.IsInternal.Should().BeFalse();
        externalContributor.TotalContributions.Should().Be(200);

        var internalContributor = result.Contributors.First(c => c.Username == "internal-user");
        internalContributor.IsInternal.Should().BeTrue();
        internalContributor.TotalContributions.Should().Be(100);
    }

    [Fact]
    public async Task GetTopContributorsAsync_ShouldHandleUserDetailFailure()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=1&per_page=100",
            new object[] { });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/members?page=2&per_page=100",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated",
            new[] { new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 } });
        mockHandler.AddJsonResponse(
            "https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated",
            new object[] { });

        mockHandler.AddJsonResponse(
            "https://api.github.com/repos/test-org/repo1/contributors?per_page=100",
            new[] { new { login = "user1", contributions = 100 } });

        mockHandler.AddErrorResponse("https://api.github.com/users/user1", HttpStatusCode.NotFound);

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.github.com") };
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var service = CreateService();

        // Act
        var result = await service.GetTopContributorsAsync();

        // Assert
        result.Contributors.Should().HaveCount(1);
        var contributor = result.Contributors.First();
        contributor.Username.Should().Be("user1");
        contributor.TotalContributions.Should().Be(100);
        contributor.Followers.Should().Be(0);
        contributor.ProfileUrl.Should().Contain("github.com/user1");
    }

    [Fact]
    public async Task GetFollowerReachAsync_ShouldHandleNoContributors()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new object[] { });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetFollowerReachAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalFollowers.Should().Be(0);
        result.ContributorsAnalyzed.Should().Be(0);
        result.ContributorsFailed.Should().Be(0);
    }

    private GitHubContributorService CreateService()
    {
        var cachingService = new CachingService(Cache, Microsoft.Extensions.Options.Options.Create(Options));
        var apiClient = new GitHubApiClient(MockHttpClientFactory.Object, CreateClientHelper());
        var topContributorsAnalyzer = new TopContributorsAnalyzer(
            Microsoft.Extensions.Options.Options.Create(Options),
            cachingService,
            apiClient,
            CreateRepositoryFetcher(),
            new Mock<ILogger<TopContributorsAnalyzer>>().Object);
        var followerReachAnalyzer = new FollowerReachAnalyzer(
            Microsoft.Extensions.Options.Options.Create(Options),
            cachingService,
            apiClient,
            CreateRepositoryFetcher(),
            new Mock<ILogger<FollowerReachAnalyzer>>().Object);

        return new GitHubContributorService(
            Microsoft.Extensions.Options.Options.Create(Options),
            cachingService,
            apiClient,
            _mockLogger.Object,
            CreateRepositoryFetcher(),
            CreateContributorAnalyzer(),
            topContributorsAnalyzer,
            followerReachAnalyzer);
    }

    /// <summary>
    /// Mock HTTP message handler for testing
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses = new();
        private readonly Dictionary<string, System.Net.HttpStatusCode> _errorResponses = new();
        public int RequestCount { get; private set; }

        public void AddJsonResponse(string url, object response)
        {
            _responses[url] = System.Text.Json.JsonSerializer.Serialize(response);
        }

        public void AddErrorResponse(string url, System.Net.HttpStatusCode statusCode)
        {
            _errorResponses[url] = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var url = request.RequestUri?.ToString() ?? string.Empty;

            // Check for error responses first
            if (_errorResponses.TryGetValue(url, out var errorCode))
            {
                return new HttpResponseMessage
                {
                    StatusCode = errorCode
                };
            }

            if (_responses.TryGetValue(url, out var json))
            {
                return new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.NotFound
            };
        }
    }
}
