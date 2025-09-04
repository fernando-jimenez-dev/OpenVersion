# Database Migrator

Runs database schema migrations for OpenVersion using FluentMigrator (PostgreSQL).

## Why
- Decouple schema evolution from EF Core design-time.
- Deterministic CLI for CI/CD and local dev.
- Consistent logs and simple flags for recreate/targeted runs.

## Usage
Build + run with a connection string:

```bash
dotnet run -p Database/Migrator/Migrator.csproj -- \
  --conn "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable"
```

Recreate the database then migrate:

```bash
dotnet run -p Database/Migrator/Migrator.csproj -- \
  --conn "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable" \
  --recreate
```

Target a specific migration version:

```bash
dotnet run -p Database/Migrator/Migrator.csproj -- --conn "<cs>" --target 202509030001
```

Flags:
- `--conn`: required PostgreSQL connection string (Database selects target DB).
- `--recreate`: drop and create the DB before migrating.
- `--target`: migrate up to a specific version; omit to go to latest.

## Logging
Uses structured console logging (timestamps). Migrator also logs via FluentMigrator console sink.

## Visual Studio
Launch profiles live in `Properties/launchSettings.json` (Dev Up / Dev Recreate) for F5 convenience.

## Migrations location
Migrations are C# classes in `Database/Migrator/FluentMigrations/*` and are scanned by the runner.

