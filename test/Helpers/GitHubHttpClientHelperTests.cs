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
            .WithMessage("*HTTP 500*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        // Act
        Action act = () => new GitHubHttpClientHelper(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(_options), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOrganizationIsEmpty()
    {
        // Arrange
        var invalidOptions = new GitHubOptions
        {
            Organization = "",
            Token = "test-token"
        };

        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(invalidOptions), _mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Organization Not Configured*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOrganizationIsWhitespace()
    {
        // Arrange
        var invalidOptions = new GitHubOptions
        {
            Organization = "   ",
            Token = "test-token"
        };

        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(invalidOptions), _mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Organization Not Configured*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOrganizationIsTooLong()
    {
        // Arrange
        var invalidOptions = new GitHubOptions
        {
            Organization = new string('a', 40), // 40 characters, max is 39
            Token = "test-token"
        };

        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(invalidOptions), _mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*too long*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOrganizationHasInvalidCharacters()
    {
        // Arrange
        var invalidOptions = new GitHubOptions
        {
            Organization = "my_org", // Underscores not allowed
            Token = "test-token"
        };

        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(invalidOptions), _mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid characters*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOrganizationHasConsecutiveHyphens()
    {
        // Arrange
        var invalidOptions = new GitHubOptions
        {
            Organization = "my--org", // Consecutive hyphens not allowed
            Token = "test-token"
        };

        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(invalidOptions), _mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid characters*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOrganizationHasSpaces()
    {
        // Arrange
        var invalidOptions = new GitHubOptions
        {
            Organization = "my org",
            Token = "test-token"
        };

        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(invalidOptions), _mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid characters*");
    }

    [Fact]
    public void Constructor_ShouldAccept_ValidOrganizationWithHyphen()
    {
        // Arrange
        var validOptions = new GitHubOptions
        {
            Organization = "my-org",
            Token = "test-token"
        };

        // Act
        Action act = () => new GitHubHttpClientHelper(Options.Create(validOptions), _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureClient_ShouldThrow_WhenClientIsNull()
    {
        // Act
        Action act = () => _helper.ConfigureClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("client");
    }

    [Fact]
    public void ConfigureClient_ShouldThrowAndLogWarning_WhenTokenIsPlaceholder_DollarBrace()
    {
        // Arrange
        var optionsWithPlaceholder = new GitHubOptions
        {
            Organization = "test-org",
            Token = "${GITHUB_TOKEN}"
        };
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithPlaceholder), _mockLogger.Object);
        var client = new HttpClient();

        // Act
        Action act = () => helper.ConfigureClient(client);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*placeholder*");
    }

    [Fact]
    public void ConfigureClient_ShouldThrow_WhenTokenIsPlaceholder_YourToken()
    {
        // Arrange
        var optionsWithPlaceholder = new GitHubOptions
        {
            Organization = "test-org",
            Token = "your-token-here"
        };
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithPlaceholder), _mockLogger.Object);
        var client = new HttpClient();

        // Act
        Action act = () => helper.ConfigureClient(client);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*placeholder*");
    }

    [Fact]
    public void ConfigureClient_ShouldThrow_WhenTokenIsPlaceholder_Xxx()
    {
        // Arrange
        var optionsWithPlaceholder = new GitHubOptions
        {
            Organization = "test-org",
            Token = "xxx-replace-me"
        };
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithPlaceholder), _mockLogger.Object);
        var client = new HttpClient();

        // Act
        Action act = () => helper.ConfigureClient(client);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*placeholder*");
    }

    [Fact]
    public void ConfigureClient_ShouldThrow_WhenTokenIsPlaceholder_Asterisks()
    {
        // Arrange
        var optionsWithPlaceholder = new GitHubOptions
        {
            Organization = "test-org",
            Token = "**************"
        };
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithPlaceholder), _mockLogger.Object);
        var client = new HttpClient();

        // Act
        Action act = () => helper.ConfigureClient(client);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*placeholder*");
    }

    [Fact]
    public void ConfigureClient_ShouldThrow_WhenTokenIsPlaceholder_Literal()
    {
        // Arrange
        var optionsWithPlaceholder = new GitHubOptions
        {
            Organization = "test-org",
            Token = "placeholder"
        };
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithPlaceholder), _mockLogger.Object);
        var client = new HttpClient();

        // Act
        Action act = () => helper.ConfigureClient(client);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*placeholder*");
    }

    [Fact]
    public void ConfigureClient_ShouldLogWarning_WhenTokenIsEmpty()
    {
        // Arrange
        var optionsWithoutToken = new GitHubOptions
        {
            Organization = "test-org",
            Token = ""
        };
        var mockLogger = new Mock<ILogger<GitHubHttpClientHelper>>();
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithoutToken), mockLogger.Object);
        var client = new HttpClient();

        // Act
        helper.ConfigureClient(client);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No GitHub token")),
                null,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldThrow_WhenResponseIsNull()
    {
        // Act
        Func<Task> act = async () => await _helper.HandleResponseAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldThrowWithRateLimitInfo_When403WithHeaders()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.Add("X-RateLimit-Remaining", "0");
        response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString());

        // Act
        Func<Task> act = async () => await _helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rate Limit*")
            .WithMessage("*Requests remaining: 0*")
            .WithMessage("*minutes*");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldIncludeTokenAdvice_When403WithoutToken()
    {
        // Arrange
        var optionsWithoutToken = new GitHubOptions
        {
            Organization = "test-org",
            Token = null
        };
        var helper = new GitHubHttpClientHelper(Options.Create(optionsWithoutToken), _mockLogger.Object);
        
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.Add("X-RateLimit-Remaining", "0");

        // Act
        Func<Task> act = async () => await helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Add a GitHub Personal Access Token*")
            .WithMessage("*60 requests/hour*")
            .WithMessage("*5,000 requests/hour*");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldIncludeCacheAdvice_When403WithToken()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.Add("X-RateLimit-Remaining", "0");

        // Act
        Func<Task> act = async () => await _helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Increase CacheDurationMinutes*");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldThrow_When401()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        // Act
        Func<Task> act = async () => await _helper.HandleResponseAsync(response, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*authentication failed*")
            .WithMessage("*ghp_*");
    }

    [Fact]
    public async Task HandleResponseAsync_ShouldLogError_OnFailure()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error")
        };

        // Act
        try
        {
            await _helper.HandleResponseAsync(response, CancellationToken.None);
        }
        catch
        {
            // Expected
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void ConfigureClient_ShouldClearExistingUserAgent()
    {
        // Arrange
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ExistingAgent/1.0");

        // Act
        _helper.ConfigureClient(client);

        // Assert
        client.DefaultRequestHeaders.UserAgent.Should().HaveCount(1);
        client.DefaultRequestHeaders.UserAgent.ToString().Should().Contain("GitHubInsights");
        client.DefaultRequestHeaders.UserAgent.ToString().Should().NotContain("ExistingAgent");
    }

    [Fact]
    public void ConfigureClient_ShouldLogDebug_WhenTokenIsSet()
    {
        // Arrange
        var client = new HttpClient();

        // Act
        _helper.ConfigureClient(client);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using GitHub token")),
                null,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
