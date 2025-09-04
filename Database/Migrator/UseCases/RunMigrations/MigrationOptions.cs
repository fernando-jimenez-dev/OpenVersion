namespace Migrator.UseCases.RunMigrations;

/// <summary>
/// Options controlling how migrations are executed.
/// </summary>
/// <param name="ConnectionString">PostgreSQL connection string. The Database segment selects the target DB.</param>
/// <param name="RecreateDatabase">If true, drop and create the database before running migrations.</param>
/// <param name="TargetVersion">Optional target migration version; when null, migrates to latest.</param>
public sealed record MigrationOptions(string ConnectionString, bool RecreateDatabase = false, long? TargetVersion = null);
