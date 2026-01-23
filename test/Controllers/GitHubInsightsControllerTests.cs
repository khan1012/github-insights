using FluentAssertions;
using GitHubInsights.Controllers;
using GitHubInsights.Models;
using GitHubInsights.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GitHubInsights.Tests.Controllers;

/// <summary>
/// Tests for GitHubInsightsController to ensure proper endpoint behavior
/// </summary>
public class GitHubInsightsControllerTests
{
    private readonly Mock<IGitHubRepositoryService> _mockRepositoryService;
    private readonly Mock<IGitHubContributorService> _mockContributorService;
    private readonly Mock<ILogger<GitHubInsightsController>> _mockLogger;
    private readonly GitHubInsightsController _controller;

    public GitHubInsightsControllerTests()
    {
        _mockRepositoryService = new Mock<IGitHubRepositoryService>();
        _mockContributorService = new Mock<IGitHubContributorService>();
        _mockLogger = new Mock<ILogger<GitHubInsightsController>>();

        _controller = new GitHubInsightsController(
            _mockRepositoryService.Object,
            _mockContributorService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetRepositoryCount_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new GitHubInsightsResponse
        {
            Organization = "test-org",
            TotalRepositories = 100,
            Timestamp = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.GetRepositoryCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetRepositoryCount(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GitHubInsightsResponse>().Subject;
        response.Organization.Should().Be("test-org");
        response.TotalRepositories.Should().Be(100);

        _mockRepositoryService.Verify(
            s => s.GetRepositoryCountAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRepositoryDetails_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new RepositoryDetailsResponse
        {
            Organization = "test-org",
            TotalRepositories = 100,
            TotalStars = 1500,
            TotalForks = 800,
            Timestamp = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.GetRepositoryDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetRepositoryDetails(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<RepositoryDetailsResponse>().Subject;
        response.TotalStars.Should().Be(1500);
        response.TotalForks.Should().Be(800);
    }

    [Fact]
    public async Task GetBasicRepositoryDetails_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new RepositoryDetailsResponse
        {
            Organization = "test-org",
            TotalRepositories = 100,
            TotalInternalContributors = 0,
            TotalExternalContributors = 0,
            Timestamp = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.GetBasicRepositoryDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetBasicRepositoryDetails(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<RepositoryDetailsResponse>().Subject;
        response.TotalInternalContributors.Should().Be(0);
        response.TotalExternalContributors.Should().Be(0);
    }

    [Fact]
    public async Task GetContributorStats_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new ContributorStats
        {
            TotalInternalContributors = 50,
            TotalExternalContributors = 30,
            Timestamp = DateTime.UtcNow
        };

        _mockContributorService
            .Setup(s => s.GetContributorStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetContributorStats(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ContributorStats>().Subject;
        response.TotalInternalContributors.Should().Be(50);
        response.TotalExternalContributors.Should().Be(30);

        _mockContributorService.Verify(
            s => s.GetContributorStatsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFollowerReach_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new FollowerReach
        {
            TotalFollowers = 5000,
            ContributorsAnalyzed = 150,
            ContributorsFailed = 2,
            Timestamp = DateTime.UtcNow
        };

        _mockContributorService
            .Setup(s => s.GetFollowerReachAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetFollowerReach(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FollowerReach>().Subject;
        response.TotalFollowers.Should().Be(5000);
        response.ContributorsAnalyzed.Should().Be(150);
        response.ContributorsFailed.Should().Be(2);
    }

    [Fact]
    public async Task GetTopContributors_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new TopContributors
        {
            Organization = "test-org",
            Contributors = new List<ContributorDetail>
            {
                new ContributorDetail
                {
                    Username = "user1",
                    ProfileUrl = "https://github.com/user1",
                    AvatarUrl = "https://avatars.githubusercontent.com/u/1",
                    TotalContributions = 500,
                    RepositoriesContributedTo = 10,
                    Followers = 250,
                    IsInternal = true
                },
                new ContributorDetail
                {
                    Username = "user2",
                    ProfileUrl = "https://github.com/user2",
                    AvatarUrl = "https://avatars.githubusercontent.com/u/2",
                    TotalContributions = 300,
                    RepositoriesContributedTo = 8,
                    Followers = 150,
                    IsInternal = false
                }
            },
            Timestamp = DateTime.UtcNow
        };

        _mockContributorService
            .Setup(s => s.GetTopContributorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetTopContributors(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TopContributors>().Subject;
        response.Organization.Should().Be("test-org");
        response.Contributors.Should().HaveCount(2);
        response.Contributors[0].Username.Should().Be("user1");
        response.Contributors[0].IsInternal.Should().BeTrue();
        response.Contributors[1].Username.Should().Be("user2");
        response.Contributors[1].IsInternal.Should().BeFalse();
    }

    [Fact]
    public async Task GetDependentRepositories_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new DependentRepositories
        {
            Organization = "test-org",
            TotalDependents = 135,
            RepositoriesAnalyzed = 20,
            PackageRepositories = 9,
            TopRepositories = new List<RepositoryDependencyInfo>
            {
                new RepositoryDependencyInfo
                {
                    Name = "terraform-provider",
                    DependentCount = 50,
                    Ecosystem = "terraform"
                },
                new RepositoryDependencyInfo
                {
                    Name = "npm-package",
                    DependentCount = 35,
                    Ecosystem = "npm"
                }
            },
            Timestamp = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.GetDependentRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDependentRepositories(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DependentRepositories>().Subject;
        response.Organization.Should().Be("test-org");
        response.TotalDependents.Should().Be(135);
        response.RepositoriesAnalyzed.Should().Be(20);
        response.PackageRepositories.Should().Be(9);
        response.TopRepositories.Should().HaveCount(2);
        response.TopRepositories[0].Name.Should().Be("terraform-provider");
        response.TopRepositories[0].DependentCount.Should().Be(50);
        response.TopRepositories[0].Ecosystem.Should().Be("terraform");
        response.TopRepositories[1].Name.Should().Be("npm-package");
        response.TopRepositories[1].Ecosystem.Should().Be("npm");

        _mockRepositoryService.Verify(
            s => s.GetDependentRepositoriesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDependentRepositories_ShouldHandleEmptyTopRepositories()
    {
        // Arrange
        var expectedResponse = new DependentRepositories
        {
            Organization = "test-org",
            TotalDependents = 0,
            RepositoriesAnalyzed = 20,
            PackageRepositories = 0,
            TopRepositories = new List<RepositoryDependencyInfo>(),
            Timestamp = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.GetDependentRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDependentRepositories(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DependentRepositories>().Subject;
        response.TotalDependents.Should().Be(0);
        response.PackageRepositories.Should().Be(0);
        response.TopRepositories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDependentRepositories_ShouldPropagateServiceException()
    {
        // Arrange
        _mockRepositoryService
            .Setup(s => s.GetDependentRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("GitHub API error"));

        // Act
        var act = async () => await _controller.GetDependentRepositories(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GitHub API error");
    }

    #region GetDetailedInsights Tests

    [Fact]
    public async Task GetDetailedInsights_ShouldReturnOkWithData()
    {
        // Arrange
        var expectedResponse = new DetailedInsights
        {
            Organization = "test-org",
            TopRepositories = new List<RepositoryInsight>
            {
                new RepositoryInsight
                {
                    Name = "repo1",
                    Url = "https://github.com/test-org/repo1",
                    Description = "Test repository",
                    Language = "C#",
                    Stars = 100,
                    Forks = 50,
                    OpenIssues = 10,
                    UpdatedAt = DateTime.UtcNow,
                    ActivityScore = 1500
                }
            },
            LanguageDistribution = new List<LanguageStats>
            {
                new LanguageStats
                {
                    Language = "C#",
                    RepositoryCount = 5,
                    Percentage = 50
                }
            },
            Activity = new ActivityBreakdown
            {
                TotalEngagement = 1500,
                ActiveRepositories = 8,
                ArchivedRepositories = 2,
                AverageStarsPerRepo = 45.5,
                AverageForksPerRepo = 20.3
            },
            Timestamp = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.GetDetailedInsightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDetailedInsights(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DetailedInsights>().Subject;
        response.Organization.Should().Be("test-org");
        response.TopRepositories.Should().HaveCount(1);
        response.TopRepositories[0].Name.Should().Be("repo1");
        response.LanguageDistribution.Should().HaveCount(1);
        response.Activity.TotalEngagement.Should().Be(1500);
    }

    [Fact]
    public async Task GetDetailedInsights_ShouldHandleEmptyTopRepositories()
    {
        // Arrange
        var expectedResponse = new DetailedInsights
        {
            Organization = "test-org",
            TopRepositories = new List<RepositoryInsight>(),
            LanguageDistribution = new List<LanguageStats>(),
            Activity = new ActivityBreakdown
            {
                TotalEngagement = 0,
                ActiveRepositories = 0,
                ArchivedRepositories = 0,
                AverageStarsPerRepo = 0,
                AverageForksPerRepo = 0
            },
            Timestamp = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.GetDetailedInsightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDetailedInsights(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DetailedInsights>().Subject;
        response.TopRepositories.Should().BeEmpty();
        response.LanguageDistribution.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailedInsights_ShouldPropagateServiceException()
    {
        // Arrange
        _mockRepositoryService
            .Setup(s => s.GetDetailedInsightsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("GitHub API error"));

        // Act
        var act = async () => await _controller.GetDetailedInsights(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GitHub API error");
    }

    #endregion
}
