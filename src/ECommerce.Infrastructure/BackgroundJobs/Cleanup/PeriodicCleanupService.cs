using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs.Cleanup;

/// <summary>
/// Base class for periodic cleanup background services.
/// Runs a cleanup operation on a fixed interval inside its own DI scope.
/// </summary>
public abstract class PeriodicCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly TimeSpan _interval;

    protected PeriodicCleanupService(
        IServiceProvider serviceProvider,
        ILogger logger,
        TimeSpan interval)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = interval;
    }

    protected abstract string ServiceName { get; }

    protected abstract Task<int> RunCleanupAsync(IServiceProvider scope, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{ServiceName} started with interval {Interval}", ServiceName, _interval);

        using var timer = new PeriodicTimer(_interval);

        try
        {
            // Run once at startup, then on each tick.
            do
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var removed = await RunCleanupAsync(scope.ServiceProvider, stoppingToken);

                    if (removed > 0)
                    {
                        _logger.LogInformation("{ServiceName} removed {Count} record(s)", ServiceName, removed);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ServiceName} failed during cleanup", ServiceName);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }

        _logger.LogInformation("{ServiceName} stopped", ServiceName);
    }
}
