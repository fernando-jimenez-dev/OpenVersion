using Application.UseCases.CheckPulse;
using Application.UseCases.CheckPulse.Abstractions;
using Application.UseCases.CheckPulse.Infrastructure;

namespace WebAPI.Minimal.StartUp;

/// <summary>
/// Contains extension methods for configuring dependency injection services.
/// Provides a centralized way to register all application dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all dependencies for the application.
    /// Use this method to register services, middleware, and other components.
    /// </summary>
    /// <param name="services">The IServiceCollection to configure.</param>
    /// <returns>The configured IServiceCollection instance.</returns>
    public static IServiceCollection ConfigureWebApiDependencies(this IServiceCollection services)
    {
        return services
            .AddCheckPulseUseCase();
    }

    private static IServiceCollection AddCheckPulseUseCase(this IServiceCollection services)
    {
        services.AddScoped<ICheckPulseUseCase, CheckPulseUseCase>();
        services.AddScoped<ICheckPulseRepository, InMemoryCheckPulseRepository>();
        return services;
    }
}