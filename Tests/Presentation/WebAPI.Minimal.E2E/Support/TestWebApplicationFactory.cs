using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Minimal.E2E.Support;

public class TestWebApplicationFactory : WebApplicationFactory<WebAPI.Minimal.Program>
{
    private string? _dbPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the DbContext with a test-specific SQLite file
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<OpenVersionContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            _dbPath = Path.Combine(Path.GetTempPath(), $"openversion-e2e-{Guid.NewGuid():N}.db");
            services.AddDbContext<OpenVersionContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));

            // Replace repository to inject a small artificial delay before SaveChanges
            var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IVersionRepository));
            if (repoDescriptor is not null)
                services.Remove(repoDescriptor);
            services.AddScoped<IVersionRepository>(sp =>
            {
                var ctx = sp.GetRequiredService<OpenVersionContext>();
                return new VersionRepository(ctx, TimeSpan.FromMilliseconds(300));
            });

            // Build provider and apply migrations for a clean schema
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenVersionContext>();
            db.Database.EnsureDeleted();
            db.Database.Migrate();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        if (string.IsNullOrWhiteSpace(_dbPath)) return;

        // Best-effort cleanup with a few retries in case the file is briefly locked
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(100);
            }
        }
    }
}