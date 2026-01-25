using GitHubInsights.Configuration;
using GitHubInsights.Helpers;
using GitHubInsights.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;

namespace GitHubInsights.Tests.Services;

/// <summary>
/// Base class for GitHub service tests providing shared test infrastructure
/// </summary>
public abstract class GitHubServiceTestBase
{
    protected readonly Mock<IHttpClientFactory> MockHttpClientFactory;
    protected readonly Mock<ILogger<GitHubHttpClientHelper>> MockHelperLogger;
    protected readonly Mock<ILogger<RepositoryFetcher>> MockRepoFetcherLogger;
    protected readonly Mock<ILogger<PullRequestFetcher>> MockPRFetcherLogger;
    protected readonly Mock<ILogger<ContributorAnalyzer>> MockContributorLogger;
    protected readonly IMemoryCache Cache;
    protected readonly GitHubOptions Options;
    protected readonly PerformanceOptions PerformanceOptions;

    protected GitHubServiceTestBase()
    {
        MockHttpClientFactory = new Mock<IHttpClientFactory>();
        MockHelperLogger = new Mock<ILogger<GitHubHttpClientHelper>>();
        MockRepoFetcherLogger = new Mock<ILogger<RepositoryFetcher>>();
        MockPRFetcherLogger = new Mock<ILogger<PullRequestFetcher>>();
        MockContributorLogger = new Mock<ILogger<ContributorAnalyzer>>();
        Cache = new MemoryCache(new MemoryCacheOptions());
        Options = new GitHubOptions
        {
            Organization = "test-org",
            Token = "ghp_testtoken123",
            CacheDurationMinutes = 5
        };
        PerformanceOptions = new PerformanceOptions
        {
            MaxConcurrentRequests = 10,
            HealthCheckStaleDays = 180,
            HealthCheckAttentionDays = 30
        };
    }

    /// <summary>
    /// Creates a configured GitHubHttpClientHelper for testing
    /// </summary>
    protected GitHubHttpClientHelper CreateClientHelper()
    {
        return new GitHubHttpClientHelper(Microsoft.Extensions.Options.Options.Create(Options), MockHelperLogger.Object);
    }

    /// <summary>
    /// Creates a configured RepositoryFetcher for testing
    /// </summary>
    protected RepositoryFetcher CreateRepositoryFetcher()
    {
        var clientHelper = CreateClientHelper();
        return new RepositoryFetcher(Microsoft.Extensions.Options.Options.Create(Options), clientHelper, MockRepoFetcherLogger.Object);
    }

    /// <summary>
    /// Creates a configured PullRequestFetcher for testing
    /// </summary>
    protected PullRequestFetcher CreatePullRequestFetcher()
    {
        return new PullRequestFetcher(Microsoft.Extensions.Options.Options.Create(Options), MockPRFetcherLogger.Object);
    }

    /// <summary>
    /// Creates a configured ContributorAnalyzer for testing
    /// </summary>
    protected ContributorAnalyzer CreateContributorAnalyzer()
    {
        return new ContributorAnalyzer(
            Microsoft.Extensions.Options.Options.Create(Options),
            Microsoft.Extensions.Options.Options.Create(PerformanceOptions),
            MockContributorLogger.Object);
    }

    /// <summary>
    /// Creates a mock HTTP message handler configured to return a specific response
    /// </summary>
    /// <param name="statusCode">HTTP status code to return</param>
    /// <param name="content">Response content</param>
    /// <param name="headers">Optional response headers</param>
    protected Mock<HttpMessageHandler> CreateMockHttpHandler(
        HttpStatusCode statusCode,
        string content,
        Dictionary<string, string>? headers = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content)
        };

        if (headers != null)
        {
            foreach (var header in headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }
        }

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return mockHandler;
    }

    /// <summary>
    /// Creates a mock HTTP message handler configured to throw an exception
    /// </summary>
    protected Mock<HttpMessageHandler> CreateErrorMockHttpHandler()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        return mockHandler;
    }

    /// <summary>
    /// Configures the mock HTTP client factory to return a client with the specified handler
    /// </summary>
    protected void SetupHttpClient(Mock<HttpMessageHandler> handler)
    {
        var httpClient = new HttpClient(handler.Object);
        MockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }
}
