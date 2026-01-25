using FluentAssertions;
using GitHubInsights.Configuration;
using GitHubInsights.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace GitHubInsights.Tests.Services;

public class CachingServiceTests
{
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IOptions<GitHubOptions>> _mockOptions;
    private readonly CachingService _cachingService;

    public CachingServiceTests()
    {
        _mockCache = new Mock<IMemoryCache>();
        _mockOptions = new Mock<IOptions<GitHubOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new GitHubOptions 
        { 
            Organization = "test-org",
            CacheDurationMinutes = 5 
        });
        
        _cachingService = new CachingService(_mockCache.Object, _mockOptions.Object);
    }

    [Fact]
    public void TryGetValue_WhenValueExists_ShouldReturnTrue()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "test-value";
        object? cacheValue = expectedValue;
        
        _mockCache
            .Setup(x => x.TryGetValue(key, out cacheValue))
            .Returns(true);

        // Act
        var result = _cachingService.TryGetValue<string>(key, out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(expectedValue);
    }

    [Fact]
    public void TryGetValue_WhenValueDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var key = "non-existent-key";
        object? cacheValue = null;
        
        _mockCache
            .Setup(x => x.TryGetValue(key, out cacheValue))
            .Returns(false);

        // Act
        var result = _cachingService.TryGetValue<string>(key, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void Set_ShouldCallCacheWithCorrectExpiration()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache
            .Setup(x => x.CreateEntry(key))
            .Returns(mockCacheEntry.Object);

        // Act
        _cachingService.Set(key, value);

        // Assert
        _mockCache.Verify(x => x.CreateEntry(key), Times.Once);
        mockCacheEntry.VerifySet(x => x.AbsoluteExpirationRelativeToNow = It.IsAny<TimeSpan>(), Times.Once);
    }

    [Fact]
    public void Set_WithCustomDuration_ShouldUseProvidedDuration()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var duration = TimeSpan.FromMinutes(10);
        
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache
            .Setup(x => x.CreateEntry(key))
            .Returns(mockCacheEntry.Object);

        // Act
        _cachingService.Set(key, value, duration);

        // Assert
        _mockCache.Verify(x => x.CreateEntry(key), Times.Once);
        mockCacheEntry.VerifySet(x => x.AbsoluteExpirationRelativeToNow = duration, Times.Once);
    }
}
