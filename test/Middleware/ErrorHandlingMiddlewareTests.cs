using FluentAssertions;
using GitHubInsights.Middleware;
using GitHubInsights.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;

namespace GitHubInsights.Tests.Middleware;

/// <summary>
/// Tests for ErrorHandlingMiddleware to ensure proper error handling and responses
/// </summary>
public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnvironment;

    public ErrorHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextDelegate_WhenNoExceptionOccurs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextDelegateCalled = false;

        RequestDelegate next = (HttpContext ctx) =>
        {
            nextDelegateCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextDelegateCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_ForInvalidOperationException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Invalid operation occurred");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var apiError = JsonSerializer.Deserialize<ApiError>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiError.Should().NotBeNull();
        apiError!.Message.Should().Be("Invalid operation occurred");
        apiError.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_ForUnauthorizedAccessException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new UnauthorizedAccessException("Unauthorized access");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var apiError = JsonSerializer.Deserialize<ApiError>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiError.Should().NotBeNull();
        apiError!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_ForArgumentException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ArgumentException("Invalid argument");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var apiError = JsonSerializer.Deserialize<ApiError>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiError.Should().NotBeNull();
        apiError!.Message.Should().Be("Invalid argument");
        apiError.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_ForUnhandledException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Unexpected error");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var apiError = JsonSerializer.Deserialize<ApiError>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiError.Should().NotBeNull();
        apiError!.Message.Should().Be("An internal server error occurred. Please check the logs for details.");
        apiError.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeStackTrace_InDevelopmentEnvironment()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var apiError = JsonSerializer.Deserialize<ApiError>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiError.Should().NotBeNull();
        apiError!.Details.Should().NotBeNullOrEmpty();
        apiError.Details.Should().Contain("at GitHubInsights.Tests.Middleware");
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotIncludeStackTrace_InProductionEnvironment()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var apiError = JsonSerializer.Deserialize<ApiError>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiError.Should().NotBeNull();
        apiError!.Details.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var testException = new InvalidOperationException("Test exception");

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw testException;
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                testException,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnCamelCaseJson()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Test");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        // Should contain camelCase properties
        responseBody.Should().Contain("\"message\"");
        responseBody.Should().Contain("\"statusCode\"");

        // Should NOT contain PascalCase properties
        responseBody.Should().NotContain("\"Message\"");
        responseBody.Should().NotContain("\"StatusCode\"");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetContentType_ToApplicationJson()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Test");
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
    }
}
