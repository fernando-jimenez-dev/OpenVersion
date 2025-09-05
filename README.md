**OpenVersion**

OpenVersion is a small, focused service that computes the next version number for a project based on branch naming conventions and simple bumping rules. It follows Clean Cut Architecture (CCA) to keep use cases, infrastructure, and presentation concerns decoupled and testable.

**Highlights**
- Purpose: Compute semantic-like version numbers per branch (main/qa/feature/fix).
- Architecture: Clean Cut Architecture (CCA) with explicit use cases and interfaces.
- Persistence: PostgreSQL (Npgsql) with EF Core for data access; schema migrations via FluentMigrator (separate Migrator project).
- API: Minimal API with two endpoints: health check and compute-next-version.
- Testing: Unit tests and realistic E2E tests using a real Postgres database.

**Requirements**
- .NET SDK: 8.0+
- PowerShell: 5.1+ or PowerShell 7+
- PostgreSQL (local Docker or managed). Optional: psql or a GUI to inspect DB.

**Architecture (CCA)**
- Core/Application: Use cases, domain models, abstractions, and error types. No framework dependencies leak into use case logic.
- Infrastructure: EF Core repository and version-bumping rules live behind interfaces in Application.
- Presentation: Minimal API project wires dependencies and exposes endpoints.
- Guiding principles: screaming architecture, high cohesion, low coupling, explicit error modeling.

**Project Structure**
- `Source/Core/Application`: use cases, shared result/error types, EF Core context/repository, bump rules.
- `Source/Presentation/WebAPI.Minimal`: Minimal API, DI setup, endpoint mapping, Swagger.
- `Database/Migrator`: console app that runs FluentMigrator migrations (PostgreSQL).
- `Tests/*`: unit tests and E2E tests (real Postgres).

**Endpoints**
- `GET /check-pulse/`
  - Returns 200 and a simple payload when the service is responsive.
- `POST /compute-next-version/`
  - Body: `{ "branchName": "main", "context": { "isMajor": "true" } }`
  - Success: 200 with `{ "nextVersion": "<computed>" }`.
  - Handled errors: 400/409/500 depending on error type.

**Database and Migrations**
- Connection string comes from layered appsettings in the WebAPI project:
  - `appsettings.json` + `appsettings.{Environment}.json` + optional `secrets.{Environment}.json` (gitignored).
- Migrations are handled by the Migrator project (FluentMigrator), not EF CLI.
- Run locally:
  - Latest: `dotnet run -p Database/Migrator/Migrator.csproj -- --conn "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable"`
  - Recreate: `dotnet run -p Database/Migrator/Migrator.csproj -- --conn "<cs>" --recreate`

**E2E Tests**
- Uses a unique Postgres database per test run (e.g., `openversion_e2e_{guid}`).
- The test factory runs the Migrator in-process to build the schema, then wires DbContext to that DB, and drops it on dispose.

**Getting Started (Local Dev)**
- Install prerequisites: .NET 8, PowerShell, PostgreSQL (Docker is fine).
- Set `ConnectionStrings:OpenVersion` in `Source/Presentation/WebAPI.Minimal/appsettings.Development.json`.
- Run the API: `dotnet run --project Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj`.
- Swagger (dev): `http://localhost:<port>/swagger`.

**Environments**
- Development: local machine. Use `appsettings.Development.json` (committed). No secrets workflow needed.
- Testing: CI/E2E or pre‑prod target. At deploy time, the build generates a runtime `secrets.json` from the committed `secrets.template.json` using a values file.
- Production: live traffic. Same as Testing: materialize `secrets.json` from a values file during deploy. Keep materialized secrets out of git and packages.

**Docker**
- The Dockerfile under `Source/Presentation/WebAPI.Minimal/` exists for local convenience and demos. It is not intended for QA/Prod in this setup.

**Build and Tasks**
- For complete build runner documentation (targets, arguments, examples), see `Build/README.md`.

