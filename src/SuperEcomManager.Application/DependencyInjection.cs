using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SuperEcomManager.Application.Common.Behaviors;
using System.Reflection;

namespace SuperEcomManager.Application;

/// <summary>
/// Dependency injection configuration for the Application layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly), assembly);

        // Register pipeline behaviors in order
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
