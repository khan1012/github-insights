using System.Net;
using System.Text.Json;
using GitHubInsights.Models;

namespace GitHubInsights.Middleware;

/// <summary>
/// Global error handling middleware
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Ensure we haven't already started writing the response
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started, cannot handle exception");
            return;
        }

        context.Response.ContentType = "application/json";

        // Handle configuration validation errors specially
        string message;
        HttpStatusCode statusCode;

        if (exception is Microsoft.Extensions.Options.OptionsValidationException optionsEx)
        {
            statusCode = HttpStatusCode.BadRequest;
            
            // Extract the validation error message
            var validationErrors = optionsEx.Failures.ToList();
            
            if (validationErrors.Any(f => f.Contains("Organization")))
            {
                message = "❌ GitHub Organization Not Configured\n\n" +
                         "The Organization field is empty in appsettings.json.\n\n" +
                         "To fix this:\n" +
                         "1. Open appsettings.json\n" +
                         "2. Set \"Organization\": \"your-org-name\"\n" +
                         "3. Use the exact organization name from GitHub\n" +
                         "   Example: \"microsoft\", \"google\", \"facebook\"\n" +
                         "4. Restart the application";
            }
            else if (validationErrors.Any(f => f.Contains("Token")))
            {
                message = "❌ Invalid GitHub Token Configuration\n\n" +
                         "Please check your token in appsettings.json:\n" +
                         "1. Token should start with 'ghp_'\n" +
                         "2. Generate a new token at https://github.com/settings/tokens/new\n" +
                         "3. Add 'read:org' and 'repo' scopes\n" +
                         "4. Update appsettings.json with the new token\n" +
                         "5. Restart the application";
            }
            else
            {
                message = "❌ Configuration Error\n\n" +
                         string.Join("\n", validationErrors) + "\n\n" +
                         "Please check your appsettings.json configuration.";
            }
        }
        else
        {
            statusCode = exception switch
            {
                InvalidOperationException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                ArgumentException => HttpStatusCode.BadRequest,
                HttpRequestException => HttpStatusCode.BadGateway,
                _ => HttpStatusCode.InternalServerError
            };

            message = exception.Message;
            
            // For non-development, don't show internal error details
            if (!_environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
            {
                message = "An internal server error occurred. Please check the logs for details.";
            }
        }

        context.Response.StatusCode = (int)statusCode;

        var apiError = new ApiError
        {
            Message = message,
            Details = _environment.IsDevelopment() ? exception.StackTrace : null,
            StatusCode = (int)statusCode
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(apiError, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
