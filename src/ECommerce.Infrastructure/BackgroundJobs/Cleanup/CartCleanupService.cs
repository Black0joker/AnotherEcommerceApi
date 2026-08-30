using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs.Cleanup;

/// <summary>
/// Periodically removes expired carts.
/// </summary>
public class CartCleanupService : PeriodicCleanupService
{
    private const int BatchSize = 500;

    public CartCleanupService(IServiceProvider serviceProvider, ILogger<CartCleanupService> logger)
        : base(serviceProvider, logger, TimeSpan.FromHours(1))
    {
    }

    protected override string ServiceName => "CartCleanupService";

    /// <summary>
    /// Deletes expired carts in bounded batches via bulk SQL deletes: no
    /// entity materialization (memory stays flat regardless of backlog) and
    /// each batch commits on its own instead of one huge delete transaction.
    /// CartItems are removed through the database-level FK cascade.
    /// </summary>
    protected override async Task<int> RunCleanupAsync(IServiceProvider scope, CancellationToken cancellationToken)
    {
        var context = scope.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var totalDeleted = 0;

        while (true)
        {
            var batch = await context.Carts
                .Where(c => c.ExpiresAt.HasValue && c.ExpiresAt.Value < now)
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            totalDeleted += await context.Carts
                .Where(c => batch.Contains(c.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return totalDeleted;
    }
}
