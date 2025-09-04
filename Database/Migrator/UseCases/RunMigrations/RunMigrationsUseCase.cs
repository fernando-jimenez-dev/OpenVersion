using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Reflection;

namespace Migrator.UseCases.RunMigrations;

/// <summary>
/// Runs FluentMigrator migrations against a PostgreSQL database.
/// Can ensure or recreate the target database, then migrate up to a specific or latest version.
/// </summary>
public interface IRunMigrationsUseCase
{
    /// <summary>
    /// Executes the migration flow based on the provided options.
    /// </summary>
    /// <param name="options">Execution options (connection, recreate flag, target version).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RunAsync(MigrationOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation that orchestrates database ensure/recreate and runs FluentMigrator.
/// </summary>
public sealed class RunMigrationsUseCase : IRunMigrationsUseCase
{
    private readonly ILogger<RunMigrationsUseCase> _logger;

    public RunMigrationsUseCase(ILogger<RunMigrationsUseCase> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync(MigrationOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ArgumentException("ConnectionString is required.", nameof(options.ConnectionString));

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        var targetDatabase = connectionStringBuilder.Database;

        if (string.IsNullOrWhiteSpace(targetDatabase))
            throw new InvalidOperationException("Connection string must include a Database.");

        _logger.LogInformation("Starting migrations: Host={Host} Port={Port} Database={Db} User={User} Recreate={Recreate} Target={Target}",
            connectionStringBuilder.Host, connectionStringBuilder.Port, connectionStringBuilder.Database, connectionStringBuilder.Username,
            options.RecreateDatabase, options.TargetVersion?.ToString() ?? "<latest>");

        if (options.RecreateDatabase)
        {
            _logger.LogInformation("Recreating database {Db}…", targetDatabase);
            await RecreateDatabaseAsync(connectionStringBuilder, cancellationToken);
            _logger.LogInformation("Database {Db} recreated.", targetDatabase);
        }
        else
        {
            _logger.LogInformation("Ensuring database {Db} exists…", targetDatabase);
            await EnsureDatabaseExistsAsync(connectionStringBuilder, cancellationToken);
            _logger.LogInformation("Database {Db} is present.", targetDatabase);
        }

        // Build FluentMigrator runner for this connection
        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionStringBuilder.ConnectionString)
                .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations()
            )
            .AddLogging(lb => lb.AddConsole())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(validateScopes: false);

        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        var sw = Stopwatch.StartNew();
        try
        {
            if (options.TargetVersion.HasValue)
            {
                _logger.LogInformation("Migrating up to version {Version}…", options.TargetVersion.Value);
                runner.MigrateUp(options.TargetVersion.Value);
            }
            else
            {
                _logger.LogInformation("Migrating up to latest…");
                runner.MigrateUp();
            }
            sw.Stop();
            _logger.LogInformation("Migrations finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Migration failed after {ElapsedMs} ms.", sw.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task EnsureDatabaseExistsAsync(NpgsqlConnectionStringBuilder connectionStringBuilder, CancellationToken cancellationToken)
    {
        var targetDatabase = connectionStringBuilder.Database;
        var serverConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionStringBuilder.ConnectionString) { Database = "postgres" };
        await using var postgreConnection = new NpgsqlConnection(serverConnectionStringBuilder.ConnectionString);
        await postgreConnection.OpenAsync(cancellationToken);
        await using var sqlCommand = postgreConnection.CreateCommand();
        sqlCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @n";
        sqlCommand.Parameters.AddWithValue("n", targetDatabase!);
        var exists = (await sqlCommand.ExecuteScalarAsync(cancellationToken)) is not null;
        if (!exists)
        {
            sqlCommand.Parameters.Clear();
            sqlCommand.CommandText = $"CREATE DATABASE \"{targetDatabase}\"";
            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task RecreateDatabaseAsync(NpgsqlConnectionStringBuilder connectionStringBuilder, CancellationToken cancellationToken)
    {
        var targetDatabase = connectionStringBuilder.Database;
        var serverConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionStringBuilder.ConnectionString) { Database = "postgres" };
        await using var postgreConnection = new NpgsqlConnection(serverConnectionStringBuilder.ConnectionString);
        await postgreConnection.OpenAsync(cancellationToken);
        await using var sqlCommand = postgreConnection.CreateCommand();

        // Terminate connections to the target DB
        sqlCommand.CommandText = @"SELECT pg_terminate_backend(pid)
                             FROM pg_stat_activity
                            WHERE datname = @n AND pid <> pg_backend_pid();";
        sqlCommand.Parameters.AddWithValue("n", targetDatabase!);
        await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        // Try DROP DATABASE WITH (FORCE); fallback to plain DROP if not supported
        sqlCommand.Parameters.Clear();
        sqlCommand.CommandText = $"DROP DATABASE IF EXISTS \"{targetDatabase}\" WITH (FORCE);";
        try { await sqlCommand.ExecuteNonQueryAsync(cancellationToken); }
        catch
        {
            sqlCommand.CommandText = $"DROP DATABASE IF EXISTS \"{targetDatabase}\";";
            await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        sqlCommand.CommandText = $"CREATE DATABASE \"{targetDatabase}\"";
        await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
