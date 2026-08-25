using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Development startup helper: applies pending EF Core migrations and seeds
/// demo data. Failures are logged rather than thrown so a missing database
/// does not prevent the API from booting (e.g. first Docker startup race).
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        try
        {
            var context = services.GetRequiredService<Context.ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(services, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }
}
