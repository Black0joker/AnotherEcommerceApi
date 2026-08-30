namespace ECommerce.Infrastructure.Persistence.Outbox;

/// <summary>
/// Durable background-job record (transactional outbox). Written in the same
/// database transaction as the business operation that enqueued the job, so
/// jobs survive process restarts and enqueueing never blocks on queue capacity.
/// </summary>
public class OutboxJob
{
    public long Id { get; set; }

    /// <summary>Full CLR type name of the <c>IJob</c> implementation.</summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>JSON payload of the job (camelCase, Web defaults).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the job finished (successfully or abandoned).</summary>
    public DateTime? ProcessedAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
