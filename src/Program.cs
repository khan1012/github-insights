using AspNetCoreRateLimit;
using GitHubInsights.Configuration;
using GitHubInsights.Helpers;
using GitHubInsights.Middleware;
using GitHubInsights.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/github-insights-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure strongly-typed settings with validation on startup
builder.Services.AddOptions<GitHubOptions>()
    .BindConfiguration(GitHubOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<PerformanceOptions>()
    .BindConfiguration(PerformanceOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ResilienceOptions>()
    .BindConfiguration(ResilienceOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

// Configure IP rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configure Swagger/OpenAPI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "GitHub Insights API";
        document.Info.Version = "v1";
        document.Info.Description = "API for analyzing GitHub organization metrics and insights";
        document.Info.Contact = new()
        {
            Name = "GitHub Insights",
            Url = new Uri("https://github.com/your-repo")
        };
        return Task.CompletedTask;
    });
});

// Configure HttpClient with Polly resilience policies
var resilienceOptions = builder.Configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();

builder.Services.AddHttpClient("GitHubApi")
    .AddPolicyHandler(GetRetryPolicy(resilienceOptions))
    .AddPolicyHandler(GetCircuitBreakerPolicy(resilienceOptions))
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddHttpClient();

// Register GitHubHttpClientHelper for dependency injection
builder.Services.AddScoped<GitHubHttpClientHelper>();

// Register abstraction layers following SOLID principles
builder.Services.AddScoped<ICachingService, CachingService>();
builder.Services.AddScoped<IGitHubApiClient, GitHubApiClient>();

// Register specialized analyzers and fetchers
builder.Services.AddScoped<IRepositoryFetcher, RepositoryFetcher>();
builder.Services.AddScoped<IPullRequestFetcher, PullRequestFetcher>();
builder.Services.AddScoped<IContributorAnalyzer, ContributorAnalyzer>();
builder.Services.AddScoped<ITopContributorsAnalyzer, TopContributorsAnalyzer>();
builder.Services.AddScoped<IFollowerReachAnalyzer, FollowerReachAnalyzer>();
builder.Services.AddScoped<IRepositoryDependencyAnalyzer, RepositoryDependencyAnalyzer>();
builder.Services.AddScoped<IRepositoryHealthAnalyzer, RepositoryHealthAnalyzer>();

// Register domain-specific services
builder.Services.AddScoped<IGitHubRepositoryService, GitHubRepositoryService>();
builder.Services.AddScoped<IGitHubContributorService, GitHubContributorService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<GitHubHealthCheck>("github_api");

// Configure CORS from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Add response caching
builder.Services.AddResponseCaching();


var app = builder.Build();

try
{
    Log.Information("Starting GitHub Insights API...");

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "GitHub Insights API v1");
            options.RoutePrefix = "swagger";
        });
    }

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "no-referrer");
        
        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }
        
        await next();
    });

    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseIpRateLimiting();
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    app.UseResponseCaching();

    // Serve static files
    app.UseDefaultFiles();
    app.UseStaticFiles();

    // Map controllers
    app.MapControllers();

    // Health check endpoints with detailed response
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow
                }));
        }
    });

    Log.Information("Application started successfully");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Swagger UI available at: /api-docs");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Polly policy helpers
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResilienceOptions options)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            options.MaxRetries,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(options.BaseDelaySeconds, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning(
                    "Retry {RetryCount} after {Delay}s due to {StatusCode}",
                    retryCount,
                    timespan.TotalSeconds,
                    outcome.Result?.StatusCode);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResilienceOptions options)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            options.CircuitBreakerThreshold,
            TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
            onBreak: (outcome, duration) =>
            {
                Log.Error(
                    "Circuit breaker opened for {Duration}s due to {StatusCode}",
                    duration.TotalSeconds,
                    outcome.Result?.StatusCode);
            },
            onReset: () => Log.Information("Circuit breaker reset"));
}
