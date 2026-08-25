using ECommerce.Application.Behaviors;
using ECommerce.Application.Pricing;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR with pipeline behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        // Register FluentValidation validators from the Application assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register the pricing/discount calculator (authoritative server-side totals)
        services.AddScoped<IDiscountCalculator, DiscountCalculator>();

        return services;
    }
}
