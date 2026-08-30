using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs.Cleanup;

/// <summary>
/// Periodically removes expired and revoked refresh tokens.
/// </summary>
public class RefreshTokenCleanupService : PeriodicCleanupService
{
    /// <summary>
    /// Batch size for bulk delete of stale refresh tokens. Small enough to keep
    /// lock escalation and transaction log pressure in check, large enough that
    /// each daily tick clears the backlog in a handful of passes.
    /// </summary>
    private const int BatchSize = 500;

    public RefreshTokenCleanupService(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger)
        : base(serviceProvider, logger, TimeSpan.FromHours(24))
    {
    }

    protected override string ServiceName => "RefreshTokenCleanupService";

    /// <summary>
    /// Deletes expired and revoked refresh tokens in bounded batches via bulk
    /// SQL deletes: no entity materialization (memory stays flat regardless of
    /// backlog) and each batch commits on its own instead of one huge delete
    /// transaction. Uses the existing IX_RefreshTokens_ExpiresAt index for the
    /// expiration predicate and a scan on the nullable RevokedAt column.
    /// </summary>
    protected override async Task<int> RunCleanupAsync(IServiceProvider scope, CancellationToken cancellationToken)
    {
        var context = scope.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var totalDeleted = 0;

        while (true)
        {
            var batch = await context.RefreshTokens
                .Where(t => t.ExpiresAt < now || t.RevokedAt != null)
                .OrderBy(t => t.Id)
                .Select(t => t.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            totalDeleted += await context.RefreshTokens
                .Where(t => batch.Contains(t.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return totalDeleted;
    }
}
