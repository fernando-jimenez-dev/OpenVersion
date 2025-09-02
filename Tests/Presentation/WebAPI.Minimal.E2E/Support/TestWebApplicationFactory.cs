using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Minimal.E2E.Support;

public class TestWebApplicationFactory : WebApplicationFactory<WebAPI.Minimal.Program>
{
    private string? _dbPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Load test-specific appsettings if present (no env vars)
            var assemblyDir = Path.GetDirectoryName(typeof(TestWebApplicationFactory).Assembly.Location)!;
            var testAppsettings = Path.Combine(assemblyDir, "appsettings.json");
            if (File.Exists(testAppsettings))
            {
                config.AddJsonFile(testAppsettings, optional: true, reloadOnChange: false);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Replace the DbContext with a test-specific SQLite file
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<OpenVersionContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Read connection string from configuration (tests/appsettings.json or API appsettings.json)
            using var spTemp = services.BuildServiceProvider();
            var configuration = spTemp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("OpenVersion");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("ConnectionStrings:OpenVersion is required for E2E tests.");

            services.AddDbContext<OpenVersionContext>(options =>
                options.UseNpgsql(connectionString));

            // Replace repository to inject a small artificial delay before SaveChanges
            var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IVersionRepository));
            if (repoDescriptor is not null)
                services.Remove(repoDescriptor);
            services.AddScoped<IVersionRepository>(sp =>
            {
                var ctx = sp.GetRequiredService<OpenVersionContext>();
                return new VersionRepository(ctx, TimeSpan.FromMilliseconds(300));
            });

            // Build provider and ensure clean schema for tests
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenVersionContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        // No file cleanup needed for PostgreSQL connection
    }
}
