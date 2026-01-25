using System.Net.Http.Headers;
using GitHubInsights.Configuration;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Helpers;

/// <summary>
/// Helper class for configuring HttpClient for GitHub API requests
/// Follows Single Responsibility Principle
/// </summary>
public class GitHubHttpClientHelper
{
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubHttpClientHelper> _logger;

    public GitHubHttpClientHelper(IOptions<GitHubOptions> options, ILogger<GitHubHttpClientHelper> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Validate organization name
        ValidateOrganization();
    }

    private void ValidateOrganization()
    {
        if (string.IsNullOrWhiteSpace(_options.Organization))
        {
            throw new InvalidOperationException(
                "❌ GitHub Organization Not Configured\n\n" +
                "The Organization field is empty in appsettings.json.\n\n" +
                "To fix this:\n" +
                "1. Open appsettings.json\n" +
                "2. Set \"Organization\": \"your-org-name\"\n" +
                "3. Use the exact organization name from GitHub\n" +
                "   Example: \"microsoft\", \"google\", \"facebook\"\n" +
                "4. Restart the application");
        }

        // Validate organization name format (GitHub requirements)
        if (_options.Organization.Length > 39)
        {
            throw new InvalidOperationException(
                "❌ Invalid Organization Name\n\n" +
                $"Organization name is too long ({_options.Organization.Length} characters).\n" +
                "GitHub organization names must be 39 characters or less.");
        }

        // Check for invalid characters and format (no consecutive hyphens, proper start/end)
        if (!System.Text.RegularExpressions.Regex.IsMatch(_options.Organization, @"^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$"))
        {
            throw new InvalidOperationException(
                "❌ Invalid Organization Name Format\n\n" +
                $"Organization name '{_options.Organization}' contains invalid characters.\n\n" +
                "GitHub organization names must:\n" +
                "• Start and end with alphanumeric characters\n" +
                "• Can contain single hyphens (-) between alphanumeric segments\n" +
                "• Cannot contain consecutive hyphens (--)\n" +
                "• Cannot contain spaces, underscores, or special characters\n\n" +
                "Example: 'my-org' is valid, 'my--org', 'my_org' or 'my org' are not.");
        }
    }

    /// <summary>
    /// Configures the HttpClient with required headers for GitHub API
    /// </summary>
    public void ConfigureClient(HttpClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("GitHubInsights", "1.0"));

        // Check for placeholder or invalid token patterns
        if (!string.IsNullOrEmpty(_options.Token))
        {
            // Detect common placeholder patterns
            if (_options.Token.StartsWith("${") || 
                _options.Token.Contains("your-token") || 
                _options.Token.Contains("xxx") ||
                 _options.Token.Contains("**") ||
                _options.Token == "placeholder")
            {
                _logger.LogWarning(
                    "Token appears to be a placeholder value: {Token}. Please replace with a real GitHub token.",
                    _options.Token.Length > 20 ? _options.Token[..20] + "..." : _options.Token);
                
                throw new InvalidOperationException(
                    "❌ Invalid GitHub Token Configuration\n\n" +
                    $"Your token appears to be a placeholder: '{_options.Token}'\n\n" +
                    "Please replace it with a real GitHub Personal Access Token:\n" +
                    "1. Go to https://github.com/settings/tokens/new\n" +
                    "2. Generate a token with 'read:org' and 'repo' scopes\n" +
                    "3. Copy the token (starts with 'ghp_')\n" +
                    "4. Update appsettings.json: \"Token\": \"ghp_your_actual_token\"\n" +
                    "5. Restart the application\n\n" +
                    "Or leave the Token field empty to use anonymous access (60 requests/hour)");
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.Token);
            _logger.LogDebug("Using GitHub token for authentication");
        }
        else
        {
            _logger.LogWarning(
                "No GitHub token configured. API rate limits will be lower (60/hour vs 5000/hour)");
        }
    }

    /// <summary>
    /// Handles common HTTP response errors
    /// </summary>
    public async Task<string> HandleResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        var statusCode = (int)response.StatusCode;
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // Extract rate limit headers if available
        var rateLimitRemaining = response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues)
            ? remainingValues.FirstOrDefault()
            : null;
        var rateLimitReset = response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues)
            ? resetValues.FirstOrDefault()
            : null;

        var errorMessage = statusCode switch
        {
            404 => $"❌ Organization '{_options.Organization}' not found.\n\n" +
                   "Please check:\n" +
                   "• Organization name spelling in appsettings.json\n" +
                   "• Organization exists at https://github.com/{_options.Organization}\n" +
                   "• If private, ensure your token has 'read:org' scope",
            
            403 => BuildRateLimitMessage(rateLimitRemaining, rateLimitReset),
            
            401 => "❌ GitHub authentication failed - Invalid or expired token.\n\n" +
                   "Your GitHub Personal Access Token is incorrect.\n\n" +
                   "To fix this:\n" +
                   "1. Check your token in appsettings.json (should start with 'ghp_')\n" +
                   "2. Verify the token hasn't expired at https://github.com/settings/tokens\n" +
                   "3. Generate a new token if needed with 'read:org' and 'repo' scopes\n" +
                   "4. Update appsettings.json with the new token\n" +
                   "5. Restart the application",
            
            _ => $"GitHub API error (HTTP {statusCode}): {errorContent}"
        };

        _logger.LogError("GitHub API error: Status {StatusCode}, Message: {ErrorMessage}", statusCode, errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    private string BuildRateLimitMessage(string? remaining, string? reset)
    {
        var message = "⏱️ GitHub API Rate Limit Exceeded\n\n";

        if (!string.IsNullOrEmpty(remaining))
        {
            message += $"Requests remaining: {remaining}\n";
        }

        if (!string.IsNullOrEmpty(reset) && long.TryParse(reset, out var resetTimestamp))
        {
            var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp);
            var timeUntilReset = resetTime - DateTimeOffset.UtcNow;
            
            if (timeUntilReset.TotalMinutes > 0)
            {
                message += $"Rate limit resets in: {timeUntilReset.Minutes} minutes\n\n";
            }
        }

        if (string.IsNullOrEmpty(_options.Token))
        {
            message += "🔧 Solution: Add a GitHub Personal Access Token\n\n" +
                      "Without a token, you're limited to 60 requests/hour.\n" +
                      "With a token, you get 5,000 requests/hour.\n\n" +
                      "Steps to add a token:\n" +
                      "1. Go to https://github.com/settings/tokens/new\n" +
                      "2. Generate a token with 'read:org' and 'repo' scopes\n" +
                      "3. Add it to appsettings.json in the 'GitHub:Token' field\n" +
                      "4. Restart the application";
        }
        else
        {
            message += "Even with a token, rate limits can be reached.\n\n" +
                      "To reduce API calls:\n" +
                      "• Increase CacheDurationMinutes in appsettings.json\n" +
                      "• Wait for rate limit to reset\n" +
                      "• Check if you have other apps using the same token";
        }

        return message;
    }
}
