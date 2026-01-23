using GitHubInsights.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitHubInsights.Services;

/// <summary>
/// Implementation of caching service
/// </summary>
public class CachingService : ICachingService
{
    private readonly IMemoryCache _cache;
    private readonly GitHubOptions _options;

    public CachingService(IMemoryCache cache, IOptions<GitHubOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool TryGetValue<T>(string key, out T? value) where T : class
    {
        return _cache.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value) where T : class
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.CacheDurationMinutes));

        _cache.Set(key, value, cacheOptions);
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value, TimeSpan duration) where T : class
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration);

        _cache.Set(key, value, cacheOptions);
    }
}
