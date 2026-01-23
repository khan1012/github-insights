using GitHubInsights.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace GitHubInsights.Services;

/// <summary>
/// Health check for GitHub API connectivity
/// </summary>
public class GitHubHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubHealthCheck> _logger;

    public GitHubHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<GitHubOptions> options,
        ILogger<GitHubHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubInsights", "1.0"));

            if (!string.IsNullOrEmpty(_options.Token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
            }

            // Quick check to see if we can reach GitHub API
            var response = await client.GetAsync(
                $"https://api.github.com/orgs/{_options.Organization}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("GitHub API is accessible");
            }

            var statusCode = (int)response.StatusCode;
            if (statusCode == 404)
            {
                return HealthCheckResult.Unhealthy($"Organization '{_options.Organization}' not found");
            }

            return HealthCheckResult.Degraded($"GitHub API returned status code {statusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for GitHub API");
            return HealthCheckResult.Unhealthy("Unable to connect to GitHub API", ex);
        }
    }
}
