using ECommerce.Application.Abstractions;
using ECommerce.Application.Jobs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.BackgroundJobs;
using ECommerce.Infrastructure.BackgroundJobs.Cleanup;
using ECommerce.Infrastructure.BackgroundJobs.Handlers;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Context;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

        // Configure ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Register services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Register Redis caching. A single shared multiplexer serves the
        // distributed cache, health checks, and pattern invalidation.
        // Lazy: the app still boots when Redis is temporarily unavailable;
        // operations degrade to cache misses.
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
            StackExchange.Redis.ConnectionMultiplexer.Connect(
                new StackExchange.Redis.ConfigurationOptions
                {
                    EndPoints = { redisConnectionString },
                    AbortOnConnectFail = false
                }));

        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = "ECommerce:";
        });

        // Point the Redis distributed cache at the shared connection instead
        // of letting it open a second multiplexer from a configuration string.
        services.AddOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>()
            .Configure<StackExchange.Redis.IConnectionMultiplexer>((options, connection) =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connection));

        // Register cache service
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Register repositories with caching decorators
        services.AddScoped<IUserRepository, UserRepository>();

        // Product repository with cache-aside pattern
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository>(sp =>
            new CachedProductRepository(
                sp.GetRequiredService<ProductRepository>(),
                sp.GetRequiredService<ICacheService>()));

        // Category repository with cache-aside pattern
        services.AddScoped<CategoryRepository>();
        services.AddScoped<ICategoryRepository>(sp =>
            new CachedCategoryRepository(
                sp.GetRequiredService<CategoryRepository>(),
                sp.GetRequiredService<ICacheService>()));

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register file storage
        var uploadsPath = Path.Combine(AppContext.BaseDirectory, "uploads");
        services.AddSingleton<IFileStorage>(new Storage.LocalFileStorage(uploadsPath, "/uploads"));

        // Durable background jobs (transactional outbox). Enqueue stages a row
        // in the caller's database transaction, so jobs commit atomically with
        // the business data, survive restarts, and never block the request
        // path on queue capacity. The processor polls pending rows.
        services.AddScoped<IBackgroundJobQueue, OutboxBackgroundJobQueue>();
        services.AddHostedService<OutboxJobProcessor>();

        // Register job handlers
        services.AddScoped<IJobHandler<SendEmailJob>, SendEmailJobHandler>();
        services.AddScoped<IJobHandler<SendOrderConfirmationJob>, SendOrderConfirmationJobHandler>();

        // Register periodic cleanup services
        services.AddHostedService<CartCleanupService>();
        services.AddHostedService<RefreshTokenCleanupService>();

        // Register email service
        services.AddSingleton<IEmailService, LoggingEmailService>();

        return services;
    }
}
