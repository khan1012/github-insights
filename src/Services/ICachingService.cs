namespace GitHubInsights.Services;

/// <summary>
/// Service for caching operations
/// </summary>
public interface ICachingService
{
    /// <summary>
    /// Try to get a cached value
    /// </summary>
    bool TryGetValue<T>(string key, out T? value) where T : class;

    /// <summary>
    /// Set a value in cache with configured duration
    /// </summary>
    void Set<T>(string key, T value) where T : class;

    /// <summary>
    /// Set a value in cache with custom duration
    /// </summary>
    void Set<T>(string key, T value, TimeSpan duration) where T : class;
}
