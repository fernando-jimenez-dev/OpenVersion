**OpenVersion**

OpenVersion is a small, focused service that computes the next version number for a project based on branch naming conventions and simple bumping rules. It follows Clean Cut Architecture (CCA) to keep use cases, infrastructure, and presentation concerns decoupled and testable.

**Highlights**
- **Purpose:** Compute semantic-like version numbers per branch (main/qa/feature/fix).
- **Architecture:** Clean Cut Architecture (CCA) with explicit use cases and interfaces.
- **Persistence:** PostgreSQL (Npgsql) via EF Core. Connection string loaded from appsettings.json (no env vars).
- **API:** Minimal API with two endpoints: health check and compute-next-version.
- **Testing:** Unit tests for the use case rules and the HTTP endpoints.

**Requirements**
- **.NET SDK:** 8.0+
- **dotnet-ef:** 9.0.x global tool (`dotnet tool update -g dotnet-ef`)
- **PowerShell:** 5.1+ or PowerShell 7+
- PostgreSQL (local Docker or managed, e.g., Neon). Optional: psql or a GUI to inspect DB.

**Architecture (CCA)**
- **Core/Application:** Use cases, domain models, abstractions, and error types. No framework dependencies leak into use case logic.
- **Infrastructure:** EF Core repository and version-bumping rules live behind interfaces in Application.
- **Presentation:** Minimal API project wires dependencies and exposes endpoints.
- **Guiding principles:** screaming architecture, high cohesion, low coupling, explicit error modeling.

**Project Structure**
- `Source/Core/Application`: use cases, shared result/error types, EF Core context/repository, bump rules.
- `Source/Presentation/WebAPI.Minimal`: Minimal API, DI setup, endpoint mapping, Swagger.
- `Tests/*`: unit tests for application logic and web endpoints.
- `scripts/*`: PowerShell scripts to create/apply EF Core migrations outside runtime.

**Endpoints**
- `GET /check-pulse/`
  - Returns 200 and a simple payload when the service is responsive.
- `POST /compute-next-version/`
  - Body: `{ "branchName": "main", "context": { "isMajor": "true" } }`
  - Success: 200 with `{ "nextVersion": "<computed>" }`.
  - Handled errors: 400/409/500 depending on error type. For 400/409 responses, a JSON body is returned containing the error message in the `nextVersion` field.

Notes on the first version for `main`:
- If no versions exist yet:
  - With `context.isMajor == "true"` the initial main version is `1.0.0.0`.
  - Without `isMajor`, the main minor rule yields `0.1.0.0+minor`.

Examples
- Compute next version for main (major):
  - `POST /compute-next-version/`
  - Body: `{ "branchName": "main", "context": { "isMajor": "true" } }`
- Compute for a feature branch:
  - `POST /compute-next-version/`
  - Body: `{ "branchName": "feature/card-123" }`

**Database and Migrations**
- Connection string comes from `Source/Presentation/WebAPI.Minimal/appsettings.json` under `ConnectionStrings:OpenVersion` and is injected into `OpenVersionContext` via `UseNpgsql(...)` in `Source/Presentation/WebAPI.Minimal/StartUp/ServiceCollectionExtensions.cs`.
- Runtime does not auto-apply migrations. Use EF CLI to manage migrations and generate SQL scripts if desired.

**Scripts**
- `scripts/New-Migration.ps1`
  - Adds a new EF Core migration into the Application project for PostgreSQL.
  - Usage: `./scripts/New-Migration.ps1 -Name AddSomeChange`
- `scripts/Update-Database.ps1`
  - Applies pending migrations to the configured PostgreSQL database.
  - Usage: `./scripts/Update-Database.ps1`
  - Target a specific migration: `./scripts/Update-Database.ps1 -Migration 20250830162208_InitialCreate`

Optional (SQL script generation)
- Generate an idempotent script (PostgreSQL): `dotnet ef migrations script --idempotent -o scripts/migrations.sql -c Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.OpenVersionContext -p Source/Core/Application/Application.csproj -s Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj`

**Getting Started**
- Install prerequisites: `.NET 8`, `dotnet-ef` tool, PowerShell.
- Set `ConnectionStrings:OpenVersion` in `Source/Presentation/WebAPI.Minimal/appsettings.json` (e.g., Neon connection string).
- Apply database migrations (optional for dev): `./scripts/Update-Database.ps1`.
- Run the API: `dotnet run --project Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj`.
- Browse Swagger UI (dev): `http://localhost:5000/swagger` (port depends on your profile).

**Testing**
- Run unit tests: `dotnet test -v minimal`
- Endpoint tests (E2E) use PostgreSQL; `TestWebApplicationFactory` reads `ConnectionStrings:OpenVersion` from test/appsettings.json and ensures a clean schema per run.
- Use case tests: `Tests/Core/Application.UnitTests/UseCases/*`

**Inspecting the Database**
- Use psql or a Postgres GUI (e.g., pgAdmin, TablePlus). Ensure your Neon/local connection details match `appsettings.json`.
