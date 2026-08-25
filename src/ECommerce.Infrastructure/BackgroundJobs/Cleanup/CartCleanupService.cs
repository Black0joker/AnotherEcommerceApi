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
    public CartCleanupService(IServiceProvider serviceProvider, ILogger<CartCleanupService> logger)
        : base(serviceProvider, logger, TimeSpan.FromHours(1))
    {
    }

    protected override string ServiceName => "CartCleanupService";

    protected override async Task<int> RunCleanupAsync(IServiceProvider scope, CancellationToken cancellationToken)
    {
        var context = scope.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var expiredCarts = await context.Carts
            .Where(c => c.ExpiresAt.HasValue && c.ExpiresAt.Value < now)
            .ToListAsync(cancellationToken);

        if (expiredCarts.Count == 0)
        {
            return 0;
        }

        context.Carts.RemoveRange(expiredCarts);
        await context.SaveChangesAsync(cancellationToken);

        return expiredCarts.Count;
    }
}
