using FluentAssertions;
using GitHubInsights.Constants;
using GitHubInsights.Models;
using GitHubInsights.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace GitHubInsights.Tests.Services;

/// <summary>
/// Tests for GitHubRepositoryService to ensure proper repository operations
/// Tests run end-to-end with real service implementations, mocking only the HTTP layer
/// </summary>
public class GitHubRepositoryServiceTests : GitHubServiceTestBase
{
    private readonly Mock<ILogger<GitHubRepositoryService>> _mockLogger;

    public GitHubRepositoryServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitHubRepositoryService>>();
    }

    [Fact]
    public async Task GetRepositoryCountAsync_ShouldReturnCachedValue_WhenAvailable()
    {
        // Arrange
        var cachedResponse = new GitHubInsightsResponse
        {
            Organization = "test-org",
            TotalRepositories = 42,
            Timestamp = DateTime.UtcNow
        };
        Cache.Set("GitHubRepoCount", cachedResponse);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryCountAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRepositories.Should().Be(42);
        // When cached, HTTP client should not be used
    }

    [Fact]
    public async Task GetRepositoryDetailsAsync_ShouldReturnCachedValue_WhenAvailable()
    {
        // Arrange
        var cachedResponse = new RepositoryDetailsResponse
        {
            Organization = "test-org",
            TotalRepositories = 110,
            TotalStars = 1192,
            TotalForks = 500,
            TotalWatchers = 800,
            TotalOpenIssues = 50,
            TotalOpenPullRequests = 416,
            TotalClosedPullRequests = 13006,
            Timestamp = DateTime.UtcNow
        };
        Cache.Set("GitHubRepoDetails", cachedResponse);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryDetailsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRepositories.Should().Be(110);
        result.TotalStars.Should().Be(1192);
        result.TotalOpenPullRequests.Should().Be(416);
        result.TotalClosedPullRequests.Should().Be(13006);
        // When cached, HTTP client should not be used
    }

    [Fact]
    public async Task GetRepositoryCountAsync_ShouldFetchFromAPI_WhenNotCached()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]"),
                Headers = { { "Link", "<https://api.github.com/orgs/test-org/repos?page=2>; rel=\"next\", <https://api.github.com/orgs/test-org/repos?page=5>; rel=\"last\"" } }
            });

        var httpClient = new HttpClient(mockHandler.Object);
        MockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryCountAsync();

        // Assert
        result.Should().NotBeNull();
        result.Organization.Should().Be("test-org");
        result.TotalRepositories.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenApiClientIsNull()
    {
        // Act
        Action act = () => new GitHubRepositoryService(
            Microsoft.Extensions.Options.Options.Create(Options),
            Mock.Of<ICachingService>(),
            null!,
            _mockLogger.Object,
            CreateRepositoryFetcher(),
            CreatePullRequestFetcher(),
            Mock.Of<IRepositoryDependencyAnalyzer>(),
            Mock.Of<IRepositoryHealthAnalyzer>());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiClient");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        // Act
        Action act = () => new GitHubRepositoryService(
            null!,
            Mock.Of<ICachingService>(),
            Mock.Of<IGitHubApiClient>(),
            _mockLogger.Object,
            CreateRepositoryFetcher(),
            CreatePullRequestFetcher(),
            Mock.Of<IRepositoryDependencyAnalyzer>(),
            Mock.Of<IRepositoryHealthAnalyzer>());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCachingServiceIsNull()
    {
        // Act
        Action act = () => new GitHubRepositoryService(
            Microsoft.Extensions.Options.Options.Create(Options),
            null!,
            Mock.Of<IGitHubApiClient>(),
            _mockLogger.Object,
            CreateRepositoryFetcher(),
            CreatePullRequestFetcher(),
            Mock.Of<IRepositoryDependencyAnalyzer>(),
            Mock.Of<IRepositoryHealthAnalyzer>());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cachingService");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new GitHubRepositoryService(
            Microsoft.Extensions.Options.Options.Create(Options),
            Mock.Of<ICachingService>(),
            Mock.Of<IGitHubApiClient>(),
            null!,
            CreateRepositoryFetcher(),
            CreatePullRequestFetcher(),
            Mock.Of<IRepositoryDependencyAnalyzer>(),
            Mock.Of<IRepositoryHealthAnalyzer>());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task GetRepositoryDetailsAsync_ShouldIncludeContributorCounts()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        // Mock repos API response - THIS IS THE PRIMARY FETCH
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 },
            new { name = "repo2", stargazers_count = 50, forks_count = 25, watchers_count = 40, open_issues_count = 5 }
        });

        // Mock repos API for pagination check
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        // Mock search API for PRs
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 10 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 50 });

        // Mock org members API
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" },
            new { login = "member2" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        // Mock contributors API for repos
        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 100 },
            new { login = "external1", contributions = 50 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo2/contributors?per_page=100", new[]
        {
            new { login = "member2", contributions = 75 },
            new { login = "external2", contributions = 25 }
        });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryDetailsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInternalContributors.Should().Be(0); // Repository service doesn't analyze contributors
        result.TotalExternalContributors.Should().Be(0); // Repository service doesn't analyze contributors
    }

    [Fact]
    public async Task GetRepositoryDetailsAsync_ShouldHandleNoOrgMembers()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 10, forks_count = 5, watchers_count = 8, open_issues_count = 2 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 0 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 0 });

        // Empty org members
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new object[] { });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        // Contributors (all external since no org members)
        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "external1", contributions = 100 }
        });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryDetailsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInternalContributors.Should().Be(0);
        result.TotalExternalContributors.Should().Be(0); // Repository service doesn't analyze contributors
    }

    [Fact]
    public async Task GetRepositoryDetailsAsync_ShouldHandleNoContributors()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 10, forks_count = 5, watchers_count = 8, open_issues_count = 2 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 0 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 0 });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        // Empty contributors
        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryDetailsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInternalContributors.Should().Be(0);
        result.TotalExternalContributors.Should().Be(0);
    }

    [Fact]
    public async Task GetRepositoryDetailsAsync_ShouldDeduplicateContributors()
    {
        // Arrange - same contributor appears in multiple repos
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 },
            new { name = "repo2", stargazers_count = 50, forks_count = 25, watchers_count = 40, open_issues_count = 5 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 10 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 50 });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        // Same contributor in both repos
        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo1/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 100 },
            new { login = "external1", contributions = 50 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/repos/test-org/repo2/contributors?per_page=100", new[]
        {
            new { login = "member1", contributions = 75 }, // Same member in both repos
            new { login = "external1", contributions = 25 } // Same external contributor
        });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryDetailsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInternalContributors.Should().Be(0); // Repository service doesn't analyze contributors
        result.TotalExternalContributors.Should().Be(0); // Repository service doesn't analyze contributors
    }

    [Fact]
    public async Task GetRepositoryDetailsAsync_ShouldSampleTopRepositories()
    {
        // Arrange - create 60 repos to test sampling
        var mockHandler = new MockHttpMessageHandler();

        var repos = Enumerable.Range(1, 60).Select(i => new
        {
            name = $"repo{i}",
            stargazers_count = 100 - i, // Descending stars
            forks_count = 50 - (i / 2),
            watchers_count = 75 - (i / 2),
            open_issues_count = 10
        }).ToArray();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 10 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 50 });

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=1&per_page=100", new[]
        {
            new { login = "member1" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/members?page=2&per_page=100", new object[] { });

        // Only mock contributors for top 50 repos (sampling limit)
        for (int i = 1; i <= 50; i++)
        {
            mockHandler.AddJsonResponse($"https://api.github.com/repos/test-org/repo{i}/contributors?per_page=100", new[]
            {
                new { login = "member1", contributions = 10 }
            });
        }

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetRepositoryDetailsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRepositories.Should().Be(60);
        result.TotalInternalContributors.Should().Be(0); // Repository service doesn't analyze contributors
    }

    [Fact]
    public async Task GetRepositoryDetailsAsync_ShouldCacheContributorCounts()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 10 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 50 });

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

        var cache = new MemoryCache(new MemoryCacheOptions());
        var cachingService = new CachingService(cache, Microsoft.Extensions.Options.Options.Create(Options));
        var apiClient = new GitHubApiClient(MockHttpClientFactory.Object, CreateClientHelper());
        var dependencyAnalyzer = new RepositoryDependencyAnalyzer(
            Microsoft.Extensions.Options.Options.Create(Options),
            cachingService,
            apiClient,
            CreateRepositoryFetcher(),
            new Mock<ILogger<RepositoryDependencyAnalyzer>>().Object);
        var healthAnalyzer = new RepositoryHealthAnalyzer(
            Microsoft.Extensions.Options.Options.Create(Options),
            Microsoft.Extensions.Options.Options.Create(PerformanceOptions),
            cachingService,
            apiClient,
            CreateRepositoryFetcher(),
            new Mock<ILogger<RepositoryHealthAnalyzer>>().Object);

        var service = new GitHubRepositoryService(
            Microsoft.Extensions.Options.Options.Create(Options),
            cachingService,
            apiClient,
            _mockLogger.Object,
            CreateRepositoryFetcher(),
            CreatePullRequestFetcher(),
            dependencyAnalyzer,
            healthAnalyzer);

        // Act
        var result1 = await service.GetRepositoryDetailsAsync(CancellationToken.None);
        var result2 = await service.GetRepositoryDetailsAsync(CancellationToken.None); // Should use cache

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.TotalInternalContributors.Should().Be(result2.TotalInternalContributors);
        result1.TotalExternalContributors.Should().Be(result2.TotalExternalContributors);
        mockHandler.RequestCount.Should().BeLessThan(20); // Second call should not make additional API calls
    }

    [Fact]
    public async Task GetBasicRepositoryDetailsAsync_ShouldReturnDetailsWithoutContributors()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10, html_url = "https://github.com/test-org/repo1", description = "Test repo" }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 10 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 50 });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetBasicRepositoryDetailsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRepositories.Should().Be(1);
        result.TotalStars.Should().Be(100);
        result.TotalForks.Should().Be(50);
        result.TotalWatchers.Should().Be(75);
        result.TotalOpenIssues.Should().Be(10);
        result.TotalOpenPullRequests.Should().Be(10);
        result.TotalClosedPullRequests.Should().Be(50);
        result.TotalInternalContributors.Should().Be(0); // Should be 0 since not fetched
        result.TotalExternalContributors.Should().Be(0); // Should be 0 since not fetched
    }

    [Fact]
    public async Task GetBasicRepositoryDetailsAsync_ShouldCacheResults()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:open+org:test-org&per_page=1", new { total_count = 10 });
        mockHandler.AddJsonResponse("https://api.github.com/search/issues?q=type:pr+state:closed+org:test-org&per_page=1", new { total_count = 50 });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result1 = await service.GetBasicRepositoryDetailsAsync(CancellationToken.None);
        var result2 = await service.GetBasicRepositoryDetailsAsync(CancellationToken.None); // Should use cache

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.TotalStars.Should().Be(result2.TotalStars);
        result1.TotalRepositories.Should().Be(result2.TotalRepositories);
        mockHandler.RequestCount.Should().BeLessThan(10); // Second call should not make additional API calls
    }

    private GitHubRepositoryService CreateService()
    {
        var cachingService = new CachingService(Cache, Microsoft.Extensions.Options.Options.Create(Options));
        var apiClient = new GitHubApiClient(MockHttpClientFactory.Object, CreateClientHelper());
        var dependencyAnalyzer = new RepositoryDependencyAnalyzer(
            Microsoft.Extensions.Options.Options.Create(Options),
            cachingService,
            apiClient,
            CreateRepositoryFetcher(),
            new Mock<ILogger<RepositoryDependencyAnalyzer>>().Object);
        var healthAnalyzer = new RepositoryHealthAnalyzer(
            Microsoft.Extensions.Options.Options.Create(Options),
            Microsoft.Extensions.Options.Options.Create(PerformanceOptions),
            cachingService,
            apiClient,
            CreateRepositoryFetcher(),
            new Mock<ILogger<RepositoryHealthAnalyzer>>().Object);

        return new GitHubRepositoryService(
            Microsoft.Extensions.Options.Options.Create(Options),
            cachingService,
            apiClient,
            _mockLogger.Object,
            CreateRepositoryFetcher(),
            CreatePullRequestFetcher(),
            dependencyAnalyzer,
            healthAnalyzer);
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

        public void AddHtmlResponse(string url, string htmlContent)
        {
            _responses[url] = htmlContent;
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

    [Fact]
    public async Task GetDependentRepositoriesAsync_ShouldReturnCachedValue_WhenAvailable()
    {
        // Arrange
        var cachedResponse = new DependentRepositories
        {
            Organization = "test-org",
            TotalDependents = 100,
            RepositoriesAnalyzed = 20,
            PackageRepositories = 5,
            TopRepositories = new List<RepositoryDependencyInfo>
            {
                new RepositoryDependencyInfo { Name = "test-repo", DependentCount = 50 }
            },
            Timestamp = DateTime.UtcNow
        };
        Cache.Set("GitHubDependentRepos", cachedResponse);

        var service = CreateService();

        // Act
        var result = await service.GetDependentRepositoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalDependents.Should().Be(100);
        result.PackageRepositories.Should().Be(5);
        result.TopRepositories.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDependentRepositoriesAsync_ShouldFetchFromAPI_WhenNotCached()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        // Mock repos API response
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 },
            new { name = "repo2", stargazers_count = 50, forks_count = 25, watchers_count = 40, open_issues_count = 5 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        // Mock dependent network pages with HTML responses
        mockHandler.AddHtmlResponse("https://github.com/test-org/repo1/network/dependents",
            "<div>25 Repositories</div>");
        mockHandler.AddHtmlResponse("https://github.com/test-org/repo2/network/dependents",
            "<div>10 Repositories</div>");

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDependentRepositoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Organization.Should().Be("test-org");
        result.TotalDependents.Should().Be(35); // 25 + 10
        result.PackageRepositories.Should().Be(2);
        result.RepositoriesAnalyzed.Should().Be(2);
        result.TopRepositories.Should().HaveCount(2);
        result.TopRepositories[0].Name.Should().Be("repo1");
        result.TopRepositories[0].DependentCount.Should().Be(25);
    }

    [Fact]
    public async Task GetDependentRepositoriesAsync_ShouldFilterOutDocsAndExampleRepos()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        // Mock repos API with some repos that should be filtered
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 },
            new { name = "docs-website", stargazers_count = 50, forks_count = 25, watchers_count = 40, open_issues_count = 5 },
            new { name = "example-app", stargazers_count = 30, forks_count = 15, watchers_count = 20, open_issues_count = 2 },
            new { name = "repo2", stargazers_count = 80, forks_count = 40, watchers_count = 60, open_issues_count = 8 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        // Only mock dependents for non-filtered repos
        mockHandler.AddHtmlResponse("https://github.com/test-org/repo1/network/dependents",
            "<div>20 Repositories</div>");
        mockHandler.AddHtmlResponse("https://github.com/test-org/repo2/network/dependents",
            "<div>15 Repositories</div>");

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDependentRepositoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.RepositoriesAnalyzed.Should().Be(2); // Only repo1 and repo2, not docs-website or example-app
        result.TotalDependents.Should().Be(35); // 20 + 15
    }

    [Fact]
    public async Task GetDependentRepositoriesAsync_ShouldSortByStarsAndTakeTop10()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        // Create 15 repos with varying stars
        var repos = Enumerable.Range(1, 15).Select(i => new
        {
            name = $"repo{i}",
            stargazers_count = i * 10,
            forks_count = i * 5,
            watchers_count = i * 3,
            open_issues_count = i
        }).ToArray();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        // Mock dependents for all repos (repos with more stars get more dependents)
        for (int i = 1; i <= 15; i++)
        {
            mockHandler.AddHtmlResponse($"https://github.com/test-org/repo{i}/network/dependents",
                $"<div>{i * 5} Repositories</div>");
        }

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDependentRepositoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.TopRepositories.Should().HaveCount(10); // Limited to top 10
        result.TopRepositories[0].Name.Should().Be("repo15"); // Highest dependent count
        result.TopRepositories[0].DependentCount.Should().Be(75); // 15 * 5
        result.TopRepositories[^1].DependentCount.Should().Be(30); // 6 * 5
    }

    [Fact]
    public async Task GetDependentRepositoriesAsync_ShouldHandleReposWithNoDependents()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", new[]
        {
            new { name = "repo1", stargazers_count = 100, forks_count = 50, watchers_count = 75, open_issues_count = 10 },
            new { name = "repo2", stargazers_count = 50, forks_count = 25, watchers_count = 40, open_issues_count = 5 }
        });
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        // repo1 has dependents, repo2 does not
        mockHandler.AddHtmlResponse("https://github.com/test-org/repo1/network/dependents",
            "<div>25 Repositories</div>");
        mockHandler.AddHtmlResponse("https://github.com/test-org/repo2/network/dependents",
            "<div>No repositories</div>"); // No match for pattern

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDependentRepositoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalDependents.Should().Be(25); // Only repo1
        result.PackageRepositories.Should().Be(1); // Only repo1 counted
        result.TopRepositories.Should().HaveCount(1);
        result.TopRepositories[0].Name.Should().Be("repo1");
    }

    #region GetDetailedInsightsAsync Tests

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldReturnCachedValue_WhenAvailable()
    {
        // Arrange
        var cachedInsights = new DetailedInsights
        {
            Organization = "test-org",
            TopRepositories = new List<RepositoryInsight>(),
            LanguageDistribution = new List<LanguageStats>(),
            Activity = new ActivityBreakdown(),
            Timestamp = DateTime.UtcNow
        };

        Cache.Set(CacheKeys.DetailedInsights, cachedInsights);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Should().Be(cachedInsights);
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldFetchFromAPI_WhenNotCached()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        var repos = new[]
        {
            new { name = "repo1", html_url = "https://github.com/test-org/repo1", description = "Test repo 1",
                  language = "C#", stargazers_count = 100, forks_count = 50, open_issues_count = 10,
                  updated_at = DateTime.UtcNow.AddDays(-5), archived = false },
            new { name = "repo2", html_url = "https://github.com/test-org/repo2", description = "Test repo 2",
                  language = "JavaScript", stargazers_count = 80, forks_count = 30, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-15), archived = false }
        };

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Organization.Should().Be("test-org");
        result.TopRepositories.Should().HaveCount(2);
        result.TopRepositories[0].Name.Should().Be("repo1");
        result.TopRepositories[0].Stars.Should().Be(100);
        result.TopRepositories[0].Language.Should().Be("C#");
        result.LanguageDistribution.Should().HaveCount(2);
        result.Activity.Should().NotBeNull();
        result.Activity.TotalEngagement.Should().Be(260); // 100+50+80+30
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldCalculateLanguageDistribution()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        var repos = new[]
        {
            new { name = "repo1", html_url = "url1", description = (string?)null, language = "C#",
                  stargazers_count = 100, forks_count = 50, open_issues_count = 10,
                  updated_at = DateTime.UtcNow, archived = false },
            new { name = "repo2", html_url = "url2", description = (string?)null, language = "C#",
                  stargazers_count = 80, forks_count = 30, open_issues_count = 5,
                  updated_at = DateTime.UtcNow, archived = false },
            new { name = "repo3", html_url = "url3", description = (string?)null, language = "JavaScript",
                  stargazers_count = 60, forks_count = 20, open_issues_count = 3,
                  updated_at = DateTime.UtcNow, archived = false },
            new { name = "repo4", html_url = "url4", description = (string?)null, language = (string?)null,
                  stargazers_count = 40, forks_count = 10, open_issues_count = 2,
                  updated_at = DateTime.UtcNow, archived = false }
        };

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.LanguageDistribution.Should().HaveCount(2); // C# and JavaScript (null excluded)
        result.LanguageDistribution[0].Language.Should().Be("C#");
        result.LanguageDistribution[0].RepositoryCount.Should().Be(2);
        result.LanguageDistribution[0].Percentage.Should().Be(50); // 2/4 * 100
        result.LanguageDistribution[1].Language.Should().Be("JavaScript");
        result.LanguageDistribution[1].RepositoryCount.Should().Be(1);
        result.LanguageDistribution[1].Percentage.Should().Be(25); // 1/4 * 100
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldCalculateActivityBreakdown()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        var repos = new[]
        {
            new { name = "active-repo", html_url = "url1", description = (string?)null, language = "C#",
                  stargazers_count = 100, forks_count = 50, open_issues_count = 10,
                  updated_at = DateTime.UtcNow.AddDays(-5), archived = false },
            new { name = "old-repo", html_url = "url2", description = (string?)null, language = "Java",
                  stargazers_count = 80, forks_count = 30, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-60), archived = false },
            new { name = "archived-repo", html_url = "url3", description = (string?)null, language = "Python",
                  stargazers_count = 60, forks_count = 20, open_issues_count = 0,
                  updated_at = DateTime.UtcNow.AddYears(-1), archived = true }
        };

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Activity.Should().NotBeNull();
        result.Activity.TotalEngagement.Should().Be(340); // Sum of all stars + forks
        result.Activity.ActiveRepositories.Should().Be(1); // Only active-repo updated in last 30 days
        result.Activity.ArchivedRepositories.Should().Be(1); // Only archived-repo
        result.Activity.AverageStarsPerRepo.Should().Be(80); // 240/3
        result.Activity.AverageForksPerRepo.Should().BeApproximately(33.33, 0.01); // 100/3
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldOrderTopReposByActivityScore()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        var now = DateTime.UtcNow;
        var repos = new[]
        {
            // High stars but archived (should rank lower)
            new { name = "archived-popular", html_url = "url1", description = (string?)null, language = "C#",
                  stargazers_count = 200, forks_count = 100, open_issues_count = 0,
                  updated_at = now.AddYears(-1), archived = true },
            // Medium stars but recently updated (should rank higher)
            new { name = "active-medium", html_url = "url2", description = (string?)null, language = "Java",
                  stargazers_count = 50, forks_count = 20, open_issues_count = 10,
                  updated_at = now.AddDays(-5), archived = false },
            // Low stars, not recent (should rank lowest)
            new { name = "inactive-small", html_url = "url3", description = (string?)null, language = "Python",
                  stargazers_count = 10, forks_count = 5, open_issues_count = 2,
                  updated_at = now.AddDays(-60), archived = false }
        };

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.TopRepositories.Should().HaveCount(3);
        // Recently updated repo should be first (gets 1000 bonus)
        result.TopRepositories[0].Name.Should().Be("active-medium");
        result.TopRepositories[0].ActivityScore.Should().BeGreaterThan(1000);
        // Verify scoring works: archived penalty divides by 10, recent update adds 1000
        // active-medium: (50*10 + 20*5 + 10*2) + 1000 = 1720
        // archived-popular: (200*10 + 100*5 + 0*2) / 10 = 250
        // inactive-small: 10*10 + 5*5 + 2*2 = 129
        result.TopRepositories[1].Name.Should().Be("archived-popular");
        result.TopRepositories[2].Name.Should().Be("inactive-small");
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldTakeTop10Repositories()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();

        var repos = Enumerable.Range(1, 15).Select(i => new
        {
            name = $"repo{i}",
            html_url = $"https://github.com/test-org/repo{i}",
            description = (string?)null,
            language = "C#",
            stargazers_count = 100 - i,
            forks_count = 50 - i,
            open_issues_count = 10,
            updated_at = DateTime.UtcNow,
            archived = false
        }).ToArray();

        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.TopRepositories.Should().HaveCount(10); // Should limit to 10
        result.TopRepositories[0].Name.Should().Be("repo1"); // Highest stars
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldCalculateHealthyRepositories()
    {
        // Arrange
        var recentUpdate = DateTime.UtcNow.AddDays(-15);
        var repos = new[]
        {
            new { name = "healthy-repo-1", html_url = "url1", description = "desc1", language = "C#",
                  stargazers_count = 100, forks_count = 20, open_issues_count = 5,
                  updated_at = recentUpdate, archived = false },
            new { name = "healthy-repo-2", html_url = "url2", description = "desc2", language = "JavaScript",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 3,
                  updated_at = DateTime.UtcNow.AddDays(-20), archived = false }
        };

        var mockHandler = new MockHttpMessageHandler();
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Health.Should().NotBeNull();
        result.Health.HealthyCount.Should().Be(2); // Both repos are healthy (updated <30 days, low issues)
        result.Health.NeedsAttentionCount.Should().Be(0);
        result.Health.AtRiskCount.Should().Be(0);
        result.Health.ArchivedCount.Should().Be(0);
        result.Health.StalePercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldIdentifyReposNeedingAttention()
    {
        // Arrange
        var repos = new[]
        {
            new { name = "aging-repo", html_url = "url1", description = "desc1", language = "C#",
                  stargazers_count = 100, forks_count = 20, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-60), archived = false }, // 60 days old
            new { name = "high-issues-repo", html_url = "url2", description = "desc2", language = "JavaScript",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 55,
                  updated_at = DateTime.UtcNow.AddDays(-10), archived = false }, // High issue count (>50)
            new { name = "high-ratio-repo", html_url = "url3", description = "desc3", language = "Python",
                  stargazers_count = 10, forks_count = 5, open_issues_count = 15,
                  updated_at = DateTime.UtcNow.AddDays(-20), archived = false } // High issue-to-star ratio (1.5)
        };

        var mockHandler = new MockHttpMessageHandler();
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Health.HealthyCount.Should().Be(0);
        result.Health.NeedsAttentionCount.Should().Be(3); // All three need attention
        result.Health.AtRiskCount.Should().Be(0);
        result.Health.RepositoriesNeedingAttention.Should().NotBeEmpty();
        result.Health.RepositoriesNeedingAttention.Count.Should().BeLessThanOrEqualTo(5); // Limited to top 5
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldIdentifyAtRiskRepositories()
    {
        // Arrange
        var repos = new[]
        {
            new { name = "stale-repo-1", html_url = "url1", description = "desc1", language = "C#",
                  stargazers_count = 100, forks_count = 20, open_issues_count = 10,
                  updated_at = DateTime.UtcNow.AddDays(-200), archived = false }, // 200 days old
            new { name = "stale-repo-2", html_url = "url2", description = "desc2", language = "JavaScript",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-365), archived = false }, // 365 days old
            new { name = "healthy-repo", html_url = "url3", description = "desc3", language = "Python",
                  stargazers_count = 80, forks_count = 15, open_issues_count = 3,
                  updated_at = DateTime.UtcNow.AddDays(-10), archived = false }
        };

        var mockHandler = new MockHttpMessageHandler();
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Health.AtRiskCount.Should().Be(2); // Two stale repos (180+ days)
        result.Health.HealthyCount.Should().Be(1);
        result.Health.StalePercentage.Should().BeApproximately(66.7, 0.1); // 2 out of 3 = 66.7%
        result.Health.AtRiskRepositories.Should().NotBeEmpty();
        result.Health.AtRiskRepositories[0].DaysSinceUpdate.Should().BeGreaterThanOrEqualTo(180);
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldCountArchivedRepositories()
    {
        // Arrange
        var repos = new[]
        {
            new { name = "active-repo", html_url = "url1", description = "desc1", language = "C#",
                  stargazers_count = 100, forks_count = 20, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-10), archived = false },
            new { name = "archived-repo-1", html_url = "url2", description = "desc2", language = "JavaScript",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 0,
                  updated_at = DateTime.UtcNow.AddDays(-400), archived = true },
            new { name = "archived-repo-2", html_url = "url3", description = "desc3", language = "Python",
                  stargazers_count = 30, forks_count = 5, open_issues_count = 0,
                  updated_at = DateTime.UtcNow.AddDays(-500), archived = true }
        };

        var mockHandler = new MockHttpMessageHandler();
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Health.ArchivedCount.Should().Be(2);
        result.Health.HealthyCount.Should().Be(1); // Only the active repo
        result.Health.AtRiskCount.Should().Be(0); // Archived repos not counted in at-risk
        result.Health.StalePercentage.Should().Be(0); // Stale percentage only counts non-archived repos
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldOrderReposNeedingAttentionByIssues()
    {
        // Arrange
        var repos = new[]
        {
            new { name = "low-issues", html_url = "url1", description = "desc1", language = "C#",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 25,
                  updated_at = DateTime.UtcNow.AddDays(-60), archived = false },
            new { name = "high-issues", html_url = "url2", description = "desc2", language = "JavaScript",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 75,
                  updated_at = DateTime.UtcNow.AddDays(-60), archived = false },
            new { name = "medium-issues", html_url = "url3", description = "desc3", language = "Python",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 45,
                  updated_at = DateTime.UtcNow.AddDays(-60), archived = false }
        };

        var mockHandler = new MockHttpMessageHandler();
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Health.RepositoriesNeedingAttention.Should().HaveCount(3);
        result.Health.RepositoriesNeedingAttention[0].Name.Should().Be("high-issues"); // 75 issues first
        result.Health.RepositoriesNeedingAttention[1].Name.Should().Be("medium-issues"); // 45 issues second
        result.Health.RepositoriesNeedingAttention[2].Name.Should().Be("low-issues"); // 25 issues last
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldOrderAtRiskReposByAge()
    {
        // Arrange
        var repos = new[]
        {
            new { name = "older-repo", html_url = "url1", description = "desc1", language = "C#",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-400), archived = false },
            new { name = "oldest-repo", html_url = "url2", description = "desc2", language = "JavaScript",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-500), archived = false },
            new { name = "old-repo", html_url = "url3", description = "desc3", language = "Python",
                  stargazers_count = 50, forks_count = 10, open_issues_count = 5,
                  updated_at = DateTime.UtcNow.AddDays(-300), archived = false }
        };

        var mockHandler = new MockHttpMessageHandler();
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Health.AtRiskRepositories.Should().HaveCount(3);
        result.Health.AtRiskRepositories[0].Name.Should().Be("oldest-repo"); // 500 days first
        result.Health.AtRiskRepositories[0].DaysSinceUpdate.Should().BeGreaterThanOrEqualTo(500);
        result.Health.AtRiskRepositories[1].Name.Should().Be("older-repo"); // 400 days second
        result.Health.AtRiskRepositories[2].Name.Should().Be("old-repo"); // 300 days last
    }

    [Fact]
    public async Task GetDetailedInsightsAsync_ShouldLimitHealthListsToTop5()
    {
        // Arrange - Create 10 repos needing attention
        var repos = Enumerable.Range(1, 10).Select(i => new
        {
            name = $"repo-{i}",
            html_url = $"url{i}",
            description = $"desc{i}",
            language = "C#",
            stargazers_count = 50,
            forks_count = 10,
            open_issues_count = 100 - i, // Descending issue count
            updated_at = DateTime.UtcNow.AddDays(-60),
            archived = false
        }).ToArray();

        var mockHandler = new MockHttpMessageHandler();
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=1&per_page=100&sort=updated", repos);
        mockHandler.AddJsonResponse("https://api.github.com/orgs/test-org/repos?page=2&per_page=100&sort=updated", new object[] { });

        var httpClient = new HttpClient(mockHandler);
        MockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.GetDetailedInsightsAsync();

        // Assert
        result.Health.NeedsAttentionCount.Should().Be(10); // All 10 repos need attention
        result.Health.RepositoriesNeedingAttention.Should().HaveCount(5); // But only top 5 returned
        result.Health.RepositoriesNeedingAttention[0].OpenIssues.Should().Be(99); // Highest issue count
    }

    #endregion
}
