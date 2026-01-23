using GitHubInsights.Configuration;
using GitHubInsights.Helpers;
using GitHubInsights.Middleware;
using GitHubInsights.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure strongly-typed settings
// Don't validate on start to allow app to run and show friendly errors in UI
builder.Services.AddOptions<GitHubOptions>()
    .BindConfiguration(GitHubOptions.SectionName)
    .ValidateDataAnnotations();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

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

// Configure CORS with specific origins (update for production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow all in development for easier testing
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // In production, specify allowed origins
            policy.WithOrigins("https://yourdomain.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Add response caching
builder.Services.AddResponseCaching();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseResponseCaching();

// Serve static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Map controllers
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHealthChecks("/health/ready");

app.Logger.LogInformation("Application started successfully");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

app.Run();
