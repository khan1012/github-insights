using FluentAssertions;
using GitHubInsights.Configuration;
using System.ComponentModel.DataAnnotations;

namespace GitHubInsights.Tests.Configuration;

public class ConfigurationOptionsTests
{
    [Fact]
    public void GitHubOptions_ValidConfiguration_ShouldPass()
    {
        // Arrange
        var options = new GitHubOptions
        {
            Organization = "microsoft",
            Token = "ghp_test123",
            CacheDurationMinutes = 5,
            MaxRepositories = 100,
            MaxRepositoriesForContributorAnalysis = 20
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void GitHubOptions_EmptyOrganization_ShouldFail()
    {
        // Arrange
        var options = new GitHubOptions
        {
            Organization = "",
            CacheDurationMinutes = 5
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        results.Should().ContainSingle();
        results.First().ErrorMessage.Should().Contain("organization");
    }

    [Fact]
    public void GitHubOptions_InvalidCacheDuration_ShouldFail()
    {
        // Arrange
        var options = new GitHubOptions
        {
            Organization = "microsoft",
            CacheDurationMinutes = 2000 // > 1440
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        results.Should().ContainSingle();
        results.First().ErrorMessage.Should().Contain("Cache duration");
    }

    [Fact]
    public void PerformanceOptions_ValidConfiguration_ShouldPass()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxConcurrentRequests = 10,
            HealthCheckStaleDays = 180,
            HealthCheckAttentionDays = 30
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceOptions_InvalidConcurrency_ShouldFail()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxConcurrentRequests = 100, // > 50
            HealthCheckStaleDays = 180
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        results.Should().ContainSingle();
        results.First().ErrorMessage.Should().Contain("concurrent requests");
    }

    [Fact]
    public void ResilienceOptions_ValidConfiguration_ShouldPass()
    {
        // Arrange
        var options = new ResilienceOptions
        {
            MaxRetries = 3,
            BaseDelaySeconds = 2,
            CircuitBreakerThreshold = 5,
            CircuitBreakerDurationSeconds = 60,
            TimeoutSeconds = 30
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void ResilienceOptions_InvalidTimeout_ShouldFail()
    {
        // Arrange
        var options = new ResilienceOptions
        {
            MaxRetries = 3,
            TimeoutSeconds = 500 // > 300
        };

        // Act
        var results = ValidateModel(options);

        // Assert
        results.Should().ContainSingle();
        results.First().ErrorMessage.Should().Contain("Timeout");
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
