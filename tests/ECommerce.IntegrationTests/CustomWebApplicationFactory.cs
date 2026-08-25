using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (routing, authentication, validation, error
/// handling, rate limiting) with SQL Server swapped for the EF Core in-memory
/// provider and Redis replaced by an in-process cache, so the full HTTP stack
/// can be exercised without external dependencies.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ecommerce-it-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Surface server-side errors in the test output for diagnosis.
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new ConsoleErrorCaptureProvider());
        });

        // Raise rate limits so parallel/sequential test traffic is never throttled.
        builder.UseSetting("RateLimiting:Global:RequestsPerMinute", "100000");
        builder.UseSetting("RateLimiting:Auth:RequestsPerMinute", "100000");
        builder.UseSetting("RateLimiting:Reviews:RequestsPerMinute", "100000");

        // ConfigureTestServices runs AFTER the application's own service
        // registration (ConfigureServices would run before it with the minimal
        // hosting model), so removals here actually replace production services.
        builder.ConfigureTestServices(services =>
        {
            // Replace SQL Server with the in-memory EF provider. All EF-related
            // descriptors must go, otherwise both providers end up registered.
            // EF Core 8+ also registers IDbContextOptionsConfiguration<TContext>
            // services that re-apply UseSqlServer, so those must be removed too.
            var optionsConfigurationType =
                typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>);

            var efDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                            || d.ServiceType == typeof(DbContextOptions)
                            || d.ServiceType == typeof(ApplicationDbContext)
                            || d.ServiceType == optionsConfigurationType)
                .ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Replace the Redis-backed cache with an in-process implementation.
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        });
    }

    /// <summary>
    /// Ensures the Admin/Customer roles exist (normally created by the
    /// development seeder, which is intentionally skipped in tests).
    /// </summary>
    public async Task EnsureRolesAsync()
    {
        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in new[] { "Admin", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    /// <summary>
    /// Creates a user directly (bypassing the register endpoint) and assigns
    /// the Admin role. Used to exercise admin-only endpoints.
    /// </summary>
    public async Task CreateAdminUserAsync(string email, string password)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "Admin",
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create admin user: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, "Admin");
        await userManager.AddToRoleAsync(user, "Customer");
    }
}

/// <summary>
/// Minimal logger provider that captures warnings/errors in memory so failing
/// integration tests can surface the underlying server exception.
/// </summary>
internal sealed class ConsoleErrorCaptureProvider : ILoggerProvider
{
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> Messages = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName);
    public void Dispose() { }

    private sealed class CapturingLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            Messages.Enqueue($"[{logLevel}] {category}: {formatter(state, exception)}");
            if (exception is not null)
            {
                Messages.Enqueue(exception.ToString());
            }
        }
    }
}

/// <summary>
/// In-process ICacheService double replacing Redis for tests.
/// </summary>
internal sealed class InMemoryCacheService : ICacheService
{
    private sealed record Entry(object Value, DateTime ExpiresAt);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Entry> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            return Task.FromResult((T?)entry.Value);
        }

        _store.TryRemove(key, out _);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(10));
        _store[key] = new Entry(value, expiresAt);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var prefix = pattern.TrimEnd('*');
        foreach (var key in _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            _store.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory();
        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }
}
