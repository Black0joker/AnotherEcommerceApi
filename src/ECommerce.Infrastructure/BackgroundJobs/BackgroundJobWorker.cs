using System.Reflection;
using ECommerce.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted service that dequeues background jobs and dispatches them to their handlers.
/// Each job is processed inside its own DI scope so scoped services (like DbContext) are available.
/// </summary>
public class BackgroundJobWorker : BackgroundService
{
    private readonly ChannelBackgroundJobQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobWorker> _logger;

    public BackgroundJobWorker(
        ChannelBackgroundJobQueue queue,
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundJobWorker started.");

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background job {JobType}", job.GetType().Name);
            }
        }

        _logger.LogInformation("BackgroundJobWorker stopped.");
    }

    private async Task ProcessJobAsync(IJob job, CancellationToken cancellationToken)
    {
        var jobType = job.GetType();
        var handlerType = typeof(IJobHandler<>).MakeGenericType(jobType);

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType);

        if (handler is null)
        {
            _logger.LogWarning("No handler registered for job type {JobType}", jobType.Name);
            return;
        }

        var method = handlerType.GetMethod(nameof(IJobHandler<IJob>.HandleAsync))
            ?? throw new InvalidOperationException($"HandleAsync method not found on handler for {jobType.Name}");

        var result = method.Invoke(handler, new object[] { job, cancellationToken });

        if (result is Task task)
        {
            await task;
        }

        _logger.LogInformation("Completed background job {JobType}", jobType.Name);
    }
}
