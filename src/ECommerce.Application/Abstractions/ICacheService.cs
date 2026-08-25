namespace ECommerce.Application.Abstractions;

/// <summary>
/// Cache abstraction for the application layer.
/// Implements cache-aside pattern.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached value by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set a value in the cache with an optional expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove a value from the cache by key.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all values matching a pattern.
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or set a value in the cache.
    /// If the key exists, return the cached value.
    /// If not, execute the factory, cache the result, and return it.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}
