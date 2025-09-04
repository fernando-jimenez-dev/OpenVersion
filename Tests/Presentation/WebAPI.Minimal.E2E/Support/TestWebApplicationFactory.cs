using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migrator.UseCases.RunMigrations;
using Npgsql;

namespace WebAPI.Minimal.E2E.Support;

/// <summary>
/// WebApplicationFactory configured for end-to-end tests.
/// For each test run, it creates a unique PostgreSQL database, runs FluentMigrator to build the schema,
/// wires the application DbContext to that database, and drops it on dispose.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<WebAPI.Minimal.Program>
{
    private string? _databaseName;
    private string? _serverConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var assemblyDir = Path.GetDirectoryName(typeof(TestWebApplicationFactory).Assembly.Location)!;
            var e2eSettings = Path.Combine(assemblyDir, "e2e.appsettings.json");
            if (File.Exists(e2eSettings))
            {
                config.AddJsonFile(e2eSettings, optional: true, reloadOnChange: false);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Replace the DbContext with a test-specific PostgreSQL connection using a unique database per test
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OpenVersionContext>));
            if (descriptor is not null) services.Remove(descriptor);

            // Read base connection string from the composed configuration
            using var spTemp = services.BuildServiceProvider();
            var configuration = spTemp.GetRequiredService<IConfiguration>();
            var baseConnectionString = configuration.GetConnectionString("OpenVersion");
            if (string.IsNullOrWhiteSpace(baseConnectionString))
                throw new InvalidOperationException("ConnectionStrings:OpenVersion is required for E2E tests in e2e.appsettings.json.");

            var baseCsb = new NpgsqlConnectionStringBuilder(baseConnectionString);
            _databaseName = $"{baseCsb.Database}_e2e_{Guid.NewGuid():N}";
            var serverCsb = new NpgsqlConnectionStringBuilder(baseCsb.ConnectionString) { Database = "postgres" };
            _serverConnectionString = serverCsb.ConnectionString;

            // Build test connection string, disable pooling for safe drop
            var testCsb = new NpgsqlConnectionStringBuilder(baseCsb.ConnectionString)
            {
                Database = _databaseName,
                Pooling = false
            };
            var testConnectionString = testCsb.ConnectionString;

            // Run FluentMigrator against this database
            var loggerFactory = LoggerFactory.Create(lb => lb.AddSimpleConsole(o => { o.IncludeScopes = false; o.TimestampFormat = "HH:mm:ss "; }));
            var migratorLogger = loggerFactory.CreateLogger<RunMigrationsUseCase>();
            var migrator = new RunMigrationsUseCase(migratorLogger);
            
            // Delegate DB creation and migrations to the Migrator
            migrator.RunAsync(new MigrationOptions(testConnectionString, RecreateDatabase: true), CancellationToken.None)
                    .GetAwaiter().GetResult();

            // Register DbContext pointing to the per-test database
            services.AddDbContext<OpenVersionContext>(options => options.UseNpgsql(testConnectionString));

            // Replace repository to inject a small artificial delay before SaveChanges
            var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IVersionRepository));
            if (repoDescriptor is not null)
                services.Remove(repoDescriptor);
            services.AddScoped<IVersionRepository>(sp =>
            {
                var ctx = sp.GetRequiredService<OpenVersionContext>();
                return new VersionRepository(ctx, TimeSpan.FromMilliseconds(300));
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        // Drop the per-test database to keep the server tidy
        try
        {
            if (!string.IsNullOrWhiteSpace(_serverConnectionString) && !string.IsNullOrWhiteSpace(_databaseName))
            {
                NpgsqlConnection.ClearAllPools();
                using var conn = new NpgsqlConnection(_serverConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE);";
                try { cmd.ExecuteNonQuery(); }
                catch
                {
                    cmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\";";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch
        {
            // best-effort cleanup only
        }
    }
}
