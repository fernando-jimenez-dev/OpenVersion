using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migrator.UseCases.RunMigrations;

// Minimal args parsing:
// --conn <connectionString>   (required)
// --recreate                  (optional) drop/create the database before migrating
// --target <version>          (optional) migrate up to specific version

/*
Full recreate + migrate:
dotnet run -p Database/Migrator/Migrator.csproj -- --conn "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable" --recreate

Migrate to a specific version:
dotnet run -p Database/Migrator/Migrator.csproj -- --conn "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable" --target 202509030001

Normal migrate (up to latest):
dotnet run -p Database/Migrator/Migrator.csproj -- --conn "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable"

*/

string? connectionString = null;
bool recreate = false;
long? targetMigration = null;

for (int i = 0; i < args.Length; i++)
{
    var a = args[i];
    switch (a)
    {
        case "--conn":
            if (i + 1 >= args.Length) throw new ArgumentException("--conn requires a value");
            connectionString = args[++i];
            break;

        case "--recreate":
            recreate = true;
            break;

        case "--target":
            if (i + 1 >= args.Length) throw new ArgumentException("--target requires a value");
            if (!long.TryParse(args[++i], out var _targetMigration)) throw new ArgumentException("--target must be a number");
            targetMigration = _targetMigration;
            break;
    }
}

if (string.IsNullOrWhiteSpace(connectionString))
    throw new ArgumentException("--conn <connectionString> is required");

var services = new ServiceCollection()
    .AddLogging(lb => lb
        .AddSimpleConsole(o => { o.IncludeScopes = false; o.TimestampFormat = "HH:mm:ss "; })
        .SetMinimumLevel(LogLevel.Information))
    .AddSingleton<IRunMigrationsUseCase, RunMigrationsUseCase>()
    .BuildServiceProvider();

var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Migrator.Program");
logger.LogInformation("Args parsed. Recreate={Recreate} Target={Target}", recreate, targetMigration?.ToString() ?? "<latest>");

var options = new MigrationOptions(connectionString!, recreate, targetMigration);

var useCase = services.GetRequiredService<IRunMigrationsUseCase>();
await useCase.RunAsync(options);
logger.LogInformation("Migrations finished successfully.");