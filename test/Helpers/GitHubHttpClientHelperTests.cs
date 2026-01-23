using FluentAssertions;
using GitHubInsights.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using Microsoft.Extensions.Logging;
using GitHubInsights.Helpers;

namespace GitHubInsights.Tests.Helpers;

/// <summary>
/// Tests for GitHubHttpClientHelper
/// </summary>
public class GitHubHttpClientHelperTests
{
    private readonly Mock<ILogger<GitHubHttpClientHelper>> _mockLogger;
    private readonly GitHubOptions _options;
    private readonly GitHubHttpClientHelper _helper;

    public GitHubHttpClientHelperTests()
    {
        _mockLogger = new Mock<ILogger<GitHubHttpClientHelper>>();
        _options = new GitHubOptions
        {
            Organization = "test-org",
            Token = "test-token",
            CacheDurationMinutes = 5
        };
        _helper = new GitHubHttpClientHelper(Options.Create(_options), _mockLogger.Object);
    }

    [Fact]
    public void ConfigureClient_ShouldSetUserAgent()
    {
        // Arrange
        var client = new HttpClient();

        // Act
        _helper.ConfigureClient(client);

        // Assert
        client.DefaultRequestHeaders.UserAgent.Should().NotBeEmpty();
        client.DefaultRequestHeaders.UserAgent.ToString().Should().Contain("GitHubInsights");
    }

    [Fact]
    public void ConfigureClient_ShouldSetAuthorizationWhenTokenProvided()
    {
        // Arrange
        var client = new HttpClient();

        // Act
        _helper.ConfigureClient(client);

        // Assert
        client.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        client.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("test-token");
    }

    [Fact]
    public void ConfigureClient_ShouldNotSetAuthorizationWhenTokenIsNull()
    {
        // Arrange
        var optionsWithoutToken = new GitHubOptions
        {
            Organization = "test-org",
            Token = null,
            CacheDurationMinutes = 5
        };
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithoutToken), _mockLogger.Object);
        var client = new HttpClient();

        // Act
        helper.ConfigureClient(client);

        // Assert
        client.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldReturnContentWhenSuccessful()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"test\":\"data\"}")
        };

        // Act
        var content = await _helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        content.Should().Be("{\"test\":\"data\"}");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldThrowWhen404()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        // Act
        Func<Task> act = async () => await _helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldThrowWhen403()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);

        // Act
        Func<Task> act = async () => await _helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*rate limit*");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldThrowForOtherErrorCodes()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        // Act
        Func<Task> act = async () => await _helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status code 500*");
    }
}
