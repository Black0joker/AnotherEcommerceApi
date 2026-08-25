namespace ECommerce.Application.Jobs;

/// <summary>
/// Enqueues background jobs to run outside the HTTP request lifecycle.
/// </summary>
public interface IBackgroundJobQueue
{
    ValueTask EnqueueAsync(IJob job, CancellationToken cancellationToken = default);
}
