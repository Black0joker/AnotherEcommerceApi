using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Context;
using ECommerce.Infrastructure.Persistence.Repositories;
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

        // Register Redis caching
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "ECommerce:";
        });

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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
