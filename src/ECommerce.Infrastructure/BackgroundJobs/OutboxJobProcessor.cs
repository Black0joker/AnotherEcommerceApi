using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using ECommerce.Application.Jobs;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls the outbox table for pending jobs and dispatches them to their
/// handlers. Replaces the in-memory channel worker: jobs are durable and
/// retried up to an attempt cap, and handler type/method lookups are cached
/// in a ConcurrentDictionary so dispatch does not pay reflection costs per job.
/// </summary>
public class OutboxJobProcessor : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;
    private const int LastErrorMaxLength = 2000;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Job CLR types keyed by the stored FullName, resolved once per process.
    private static readonly Lazy<IReadOnlyDictionary<string, Type>> JobTypes = new(() =>
        typeof(IJob).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IJob).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t));

    // Handler type + HandleAsync MethodInfo per job type, resolved once.
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Method)> DispatchCache = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxJobProcessor> _logger;

    public OutboxJobProcessor(IServiceProvider serviceProvider, ILogger<OutboxJobProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxJobProcessor started with interval {Interval}", PollInterval);

        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            // Run once at startup, then on each tick.
            do
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox polling iteration failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }

        _logger.LogInformation("OutboxJobProcessor stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingJobs = await context.OutboxJobs
            .Where(j => j.ProcessedAtUtc == null && j.Attempts < MaxAttempts)
            .OrderBy(j => j.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingJobs.Count == 0)
        {
            return;
        }

        foreach (var entry in pendingJobs)
        {
            try
            {
                var job = Deserialize(entry);
                await DispatchAsync(job, cancellationToken);
                entry.ProcessedAtUtc = DateTime.UtcNow;
                entry.LastError = null;
            }
            catch (Exception ex)
            {
                entry.Attempts++;
                var error = ex.ToString();
                entry.LastError = error.Length > LastErrorMaxLength ? error[..LastErrorMaxLength] : error;

                if (entry.Attempts >= MaxAttempts)
                {
                    // Poison job: stop retrying but keep the record for inspection.
                    entry.ProcessedAtUtc = DateTime.UtcNow;
                    _logger.LogError(ex, "Background job {JobType} abandoned after {Attempts} attempts", entry.JobType, entry.Attempts);
                }
                else
                {
                    _logger.LogWarning(ex, "Background job {JobType} failed (attempt {Attempts}/{MaxAttempts})", entry.JobType, entry.Attempts, MaxAttempts);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IJob Deserialize(Persistence.Outbox.OutboxJob entry)
    {
        if (!JobTypes.Value.TryGetValue(entry.JobType, out var jobType))
        {
            throw new InvalidOperationException($"Unknown job type '{entry.JobType}'.");
        }

        return (IJob)(JsonSerializer.Deserialize(entry.PayloadJson, jobType, JsonOptions)
            ?? throw new InvalidOperationException($"Empty payload for job type '{entry.JobType}'."));
    }

    private async Task DispatchAsync(IJob job, CancellationToken cancellationToken)
    {
        var jobType = job.GetType();

        var (handlerType, method) = DispatchCache.GetOrAdd(jobType, static t =>
        {
            var ht = typeof(IJobHandler<>).MakeGenericType(t);
            var m = ht.GetMethod(nameof(IJobHandler<IJob>.HandleAsync), BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException($"HandleAsync method not found on handler for {t.Name}");
            return (ht, m);
        });

        // Per-job scope so each handler gets fresh scoped services (DbContext
        // etc.) and state cannot leak between jobs.
        using var jobScope = _serviceProvider.CreateScope();
        var handler = jobScope.ServiceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler registered for job type {jobType.Name}.");
        }

        var result = method.Invoke(handler, new object[] { job, cancellationToken });

        if (result is Task task)
        {
            await task;
        }
    }
}
