using System.Text.Json;
using ECommerce.Application.Jobs;
using ECommerce.Infrastructure.Persistence.Context;
using ECommerce.Infrastructure.Persistence.Outbox;

namespace ECommerce.Infrastructure.BackgroundJobs;

/// <summary>
/// Transactional-outbox job queue: enqueue stages a durable row in the
/// caller's open unit of work instead of an in-memory channel. Jobs commit
/// atomically with the business data that produced them, survive process
/// restarts, and enqueueing never blocks on queue capacity.
/// </summary>
public class OutboxBackgroundJobQueue : IBackgroundJobQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;

    public OutboxBackgroundJobQueue(ApplicationDbContext context)
    {
        _context = context;
    }

    public ValueTask EnqueueAsync(IJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        _context.OutboxJobs.Add(new OutboxJob
        {
            JobType = job.GetType().FullName!,
            PayloadJson = JsonSerializer.Serialize(job, job.GetType(), JsonOptions),
            CreatedAtUtc = DateTime.UtcNow
        });

        // Deliberately no SaveChanges here: the row is part of the caller's
        // unit of work so it commits atomically with the business operation.
        return ValueTask.CompletedTask;
    }
}
