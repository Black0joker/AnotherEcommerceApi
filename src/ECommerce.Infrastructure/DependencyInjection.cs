using ECommerce.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Infrastructure services (persistence, caching, storage, etc.) will be registered here
        return services;
    }
}
