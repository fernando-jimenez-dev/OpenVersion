using Application.UseCases.CheckPulse;
using Application.UseCases.CheckPulse.Abstractions;
using Application.UseCases.CheckPulse.Infrastructure;
using Application.UseCases.ComputeNextVersion;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;
using Application.UseCases.ComputeNextVersion.Infrastructure.VersionBumping;
using Microsoft.EntityFrameworkCore;

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
    public static IServiceCollection ConfigureWebApiDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddCheckPulseUseCase()
            .AddComputeNextVersionUseCase(configuration);
    }

    private static IServiceCollection AddCheckPulseUseCase(this IServiceCollection services)
    {
        services.AddScoped<ICheckPulseUseCase, CheckPulseUseCase>();
        services.AddScoped<ICheckPulseRepository, InMemoryCheckPulseRepository>();
        return services;
    }

    private static IServiceCollection AddComputeNextVersionUseCase(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext - PostgreSQL via Npgsql, connection string from appsettings.json only
        var connectionString = configuration.GetConnectionString("OpenVersion");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:OpenVersion is required in configuration.");

        services.AddDbContext<OpenVersionContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IComputeNextVersionUseCase, ComputeNextVersionUseCase>();
        services.AddScoped<IVersionRepository, VersionRepository>();
        services.AddScoped<IVersionBumper, VersionBumper>(c => new VersionBumper());
        return services;
    }
}