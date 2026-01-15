namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for caching operations.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries matching a pattern.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets a value in cache.
    /// If the key doesn't exist, the factory is called to create the value.
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}
