using FluentAssertions;
using GitHubInsights.Configuration;
using GitHubInsights.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;

namespace GitHubInsights.Tests.HealthChecks;

/// <summary>
/// Tests for GitHubHealthCheck to ensure proper health monitoring
/// </summary>
public class GitHubHealthCheckTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IOptions<GitHubOptions>> _mockOptions;
    private readonly Mock<ILogger<GitHubHealthCheck>> _mockLogger;
    private readonly GitHubOptions _options;

    public GitHubHealthCheckTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockOptions = new Mock<IOptions<GitHubOptions>>();
        _mockLogger = new Mock<ILogger<GitHubHealthCheck>>();

        _options = new GitHubOptions
        {
            Organization = "test-org",
            Token = "ghp_test123"
        };

        _mockOptions.Setup(o => o.Value).Returns(_options);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenGitHubApiIsAccessible()
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
                Content = new StringContent("{}")
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("GitHub API is accessible");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenOrganizationNotFound()
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
                StatusCode = HttpStatusCode.NotFound
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Organization 'test-org' not found");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenApiReturnsNonSuccessStatus()
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
                StatusCode = HttpStatusCode.Forbidden
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("GitHub API returned status code 403");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenApiReturnsRateLimitStatus()
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
                StatusCode = (HttpStatusCode)429 // Too Many Requests
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("GitHub API returned status code 429");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenExceptionOccurs()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var expectedException = new HttpRequestException("Connection refused");

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Unable to connect to GitHub API");
        result.Exception.Should().Be(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var expectedException = new HttpRequestException("Connection timeout");

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldSetAuthorizationHeader_WhenTokenProvided()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("ghp_test123");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldNotSetAuthorizationHeader_WhenTokenIsEmpty()
    {
        // Arrange
        _options.Token = string.Empty;

        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldSetUserAgentHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.UserAgent.Should().NotBeEmpty();
        capturedRequest.Headers.UserAgent.ToString().Should().Contain("GitHubInsights");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldUseCorrectApiEndpoint()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri!.ToString().Should().Be("https://api.github.com/orgs/test-org");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var healthCheck = new GitHubHealthCheck(_mockHttpClientFactory.Object, _mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Unable to connect to GitHub API");
    }
}
