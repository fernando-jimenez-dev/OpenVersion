# E2E Tests

The E2E suite runs the API in-process using `WebApplicationFactory` and a real PostgreSQL database.

## Isolation Strategy
- A unique database is created per test run (e.g., `openversion_e2e_{guid}`).
- FluentMigrator builds the schema in that database.
- The API's DbContext is wired to that connection.
- After tests, the database is dropped (best-effort, with `WITH (FORCE)` fallback).

This yields realistic provider behavior (indexes, concurrency) and safe parallelization.

## Configuration
- Base connection is read from `e2e.appsettings.json` (copied to test output) or the API appsettings as fallback.
- Example value:

```json
{
  "ConnectionStrings": {
    "OpenVersion": "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable"
  }
}
```

The user must have `CREATEDB` privilege to create/drop per-test databases.

## Running
```bash
dotnet test Tests/Presentation/WebAPI.Minimal.E2E/WebAPI.Minimal.E2E.csproj -v minimal
```

## Notes
- Connection pooling is disabled for per-test DB connections to ensure reliable teardown.
- If Postgres isn't available, tests will fail fast with 500s. You can add a preflight check to skip E2E in CI when desired.

