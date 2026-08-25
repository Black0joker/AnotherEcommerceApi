using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ECommerce.Api.HealthChecks;

/// <summary>
/// Readiness probe for the Redis dependency. A downed Redis degrades the API
/// (cache misses fall through to SQL Server) rather than taking it offline, so
/// this check reports <see cref="HealthStatus.Degraded"/> instead of Unhealthy.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = _redis.IsConnected
            ? HealthCheckResult.Healthy("Redis is reachable.")
            : HealthCheckResult.Degraded("Redis is not reachable; caching is temporarily unavailable.");

        return Task.FromResult(result);
    }
}
