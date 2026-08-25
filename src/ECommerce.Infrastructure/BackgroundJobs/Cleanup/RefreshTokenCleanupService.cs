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
    public RefreshTokenCleanupService(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger)
        : base(serviceProvider, logger, TimeSpan.FromHours(24))
    {
    }

    protected override string ServiceName => "RefreshTokenCleanupService";

    protected override async Task<int> RunCleanupAsync(IServiceProvider scope, CancellationToken cancellationToken)
    {
        var context = scope.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var staleTokens = await context.RefreshTokens
            .Where(t => t.ExpiresAt < now || t.RevokedAt != null)
            .ToListAsync(cancellationToken);

        if (staleTokens.Count == 0)
        {
            return 0;
        }

        context.RefreshTokens.RemoveRange(staleTokens);
        await context.SaveChangesAsync(cancellationToken);

        return staleTokens.Count;
    }
}
