using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        // Entity graphs contain circular navigations (Product <-> ProductCategory,
        // Category <-> SubCategories/ParentCategory). IgnoreCycles writes already-visited
        // back-references as null instead of throwing, so cache-aside actually populates.
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);
            if (data is null)
                return null;

            return JsonSerializer.Deserialize<T>(data, _jsonOptions);
        }
        catch
        {
            // If Redis is unavailable, return null (cache miss)
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var data = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };

            await _cache.SetStringAsync(key, data, options, cancellationToken);
        }
        catch
        {
            // If Redis is unavailable, silently fail (don't block the request)
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch
        {
            // If Redis is unavailable, silently fail
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            var keys = server.Keys(pattern: $"{pattern}*").ToArray();

            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
            }
        }
        catch
        {
            // If Redis is unavailable, silently fail
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        // Try to get from cache first
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        // Cache miss - get from source
        var value = await factory();

        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }
}
