using GitHubInsights.Helpers;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of GitHub API client
/// </summary>
public class GitHubApiClient : IGitHubApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GitHubHttpClientHelper _clientHelper;

    public GitHubApiClient(
        IHttpClientFactory httpClientFactory,
        GitHubHttpClientHelper clientHelper)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _clientHelper = clientHelper ?? throw new ArgumentNullException(nameof(clientHelper));
    }

    /// <inheritdoc />
    public HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        _clientHelper.ConfigureClient(client);
        return client;
    }
}
