using System.Threading.Channels;
using ECommerce.Application.Jobs;

namespace ECommerce.Infrastructure.BackgroundJobs;

/// <summary>
/// In-memory background job queue backed by a bounded channel.
/// Jobs are processed asynchronously by the <see cref="BackgroundJobWorker"/>.
/// </summary>
public class ChannelBackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<IJob> _queue;

    public ChannelBackgroundJobQueue(int capacity = 100)
    {
        _queue = Channel.CreateBounded<IJob>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async ValueTask EnqueueAsync(IJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        await _queue.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<IJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

    public IAsyncEnumerable<IJob> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}
