using GitHubInsights.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitHubInsights.Controllers;

/// <summary>
/// Controller for GitHub insights endpoints
/// </summary>
[ApiController]
[Route("api/github")]
[Produces("application/json")]
public class GitHubInsightsController : ControllerBase
{
    private readonly IGitHubRepositoryService _repositoryService;
    private readonly IGitHubContributorService _contributorService;
    private readonly ILogger<GitHubInsightsController> _logger;

    public GitHubInsightsController(
        IGitHubRepositoryService repositoryService,
        IGitHubContributorService contributorService,
        ILogger<GitHubInsightsController> logger)
    {
        _repositoryService = repositoryService;
        _contributorService = contributorService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the total number of repositories in the configured organization
    /// </summary>
    /// <returns>GitHub insights with repository count</returns>
    /// <response code="200">Returns the repository count</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("repos/count")]
    [ProducesResponseType(typeof(Models.GitHubInsightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRepositoryCount(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get repository count");

        var result = await _repositoryService.GetRepositoryCountAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets detailed repository statistics including stars, forks, and more
    /// </summary>
    /// <returns>Detailed repository statistics</returns>
    /// <response code="200">Returns detailed repository statistics</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("repos/details")]
    [ProducesResponseType(typeof(Models.RepositoryDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRepositoryDetails(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get detailed repository statistics");

        var result = await _repositoryService.GetRepositoryDetailsAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets basic repository statistics (fast) without contributor analysis
    /// </summary>
    /// <returns>Basic repository statistics</returns>
    /// <response code="200">Returns basic repository statistics</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("repos/basic")]
    [ProducesResponseType(typeof(Models.RepositoryDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBasicRepositoryDetails(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get basic repository statistics (without contributors)");

        var result = await _repositoryService.GetBasicRepositoryDetailsAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets contributor statistics (may take longer)
    /// </summary>
    /// <returns>Contributor statistics</returns>
    /// <response code="200">Returns contributor statistics</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("repos/contributors")]
    [ProducesResponseType(typeof(Models.ContributorStats), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetContributorStats(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get contributor statistics");

        var result = await _contributorService.GetContributorStatsAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets estimated follower reach across all contributors
    /// </summary>
    /// <returns>Follower reach statistics</returns>
    /// <response code="200">Returns follower reach statistics</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("repos/followerreach")]
    [ProducesResponseType(typeof(Models.FollowerReach), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFollowerReach(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get follower reach statistics");

        var result = await _contributorService.GetFollowerReachAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets top contributors with detailed information
    /// </summary>
    /// <returns>Top contributors list</returns>
    /// <response code="200">Returns top contributors</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("repos/topcontributors")]
    [ProducesResponseType(typeof(Models.TopContributors), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopContributors(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get top contributors");

        var result = await _contributorService.GetTopContributorsAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets information about repositories that depend on this organization's code
    /// </summary>
    /// <returns>Dependent repositories information</returns>
    /// <response code="200">Returns dependent repositories data</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("repos/dependents")]
    [ProducesResponseType(typeof(Models.DependentRepositories), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDependentRepositories(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get dependent repositories");

        var result = await _repositoryService.GetDependentRepositoriesAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets detailed insights including top repositories, language distribution, and activity breakdown
    /// </summary>
    /// <returns>Comprehensive detailed insights</returns>
    /// <response code="200">Returns detailed insights with top repos and language stats</response>
    /// <response code="400">If the GitHub organization is not configured or invalid</response>
    /// <response code="500">If an error occurs while fetching data</response>
    [HttpGet("insights/detailed")]
    [ProducesResponseType(typeof(Models.DetailedInsights), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetailedInsights(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get detailed insights");

        var result = await _repositoryService.GetDetailedInsightsAsync(cancellationToken);

        return Ok(result);
    }
}
