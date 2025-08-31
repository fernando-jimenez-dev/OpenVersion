**OpenVersion**

OpenVersion is a small, focused service that computes the next version number for a project based on branch naming conventions and simple bumping rules. It follows Clean Cut Architecture (CCA) to keep use cases, infrastructure, and presentation concerns decoupled and testable.

**Highlights**
- **Purpose:** Compute semantic-like version numbers per branch (main/qa/feature/fix).
- **Architecture:** Clean Cut Architecture (CCA) with explicit use cases and interfaces.
- **Persistence:** SQLite via EF Core. Migrations live in code; execution happens through scripts.
- **API:** Minimal API with two endpoints: health check and compute-next-version.
- **Testing:** Unit tests for the use case rules and the HTTP endpoints.

**Requirements**
- **.NET SDK:** 8.0+
- **dotnet-ef:** 9.0.x global tool (`dotnet tool update -g dotnet-ef`)
- **PowerShell:** 5.1+ or PowerShell 7+
- Optional: SQLite tooling (e.g., DB Browser for SQLite) to inspect `openversion.db`.

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
- Connection string is configured in `Source/Presentation/WebAPI.Minimal/StartUp/ServiceCollectionExtensions.cs:31` as `UseSqlite("Data Source=openversion.db")`.
- The SQLite file (`openversion.db`) is created by applying migrations and persists until deleted.
- Migrations are code-first and live in the Application project under `UseCases/ComputeNextVersion/Infrastructure/EntityFramework/Migrations`.
- Runtime does not auto-apply migrations; use the scripts below.

**Scripts**
- `scripts/New-Migration.ps1:1`
  - Adds a new EF Core migration into the Application project.
  - Usage: `./scripts/New-Migration.ps1 -Name AddSomeChange`
- `scripts/Update-Database.ps1:1`
  - Applies all pending migrations to the configured SQLite database (creates the file if missing).
  - Usage: `./scripts/Update-Database.ps1`
  - Target a specific migration: `./scripts/Update-Database.ps1 -Migration 20250830162208_InitialCreate`

Optional (SQL script generation)
- Generate an idempotent script: `dotnet ef migrations script --idempotent -o scripts/migrations.sql -c Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.OpenVersionContext -p Source/Core/Application/Application.csproj -s Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj`

**Getting Started**
- Install prerequisites: `.NET 8`, `dotnet-ef` tool, PowerShell.
- Apply database migrations: `./scripts/Update-Database.ps1`.
- Run the API: `dotnet run --project Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj`.
- Browse Swagger UI (dev): `http://localhost:5000/swagger` (port depends on your profile).

**Testing**
- Run unit tests: `dotnet test -v minimal`
- Endpoint tests: `Tests/Presentation/WebAPI.Minimal.UnitTests/UseCases/*`
- Use case tests: `Tests/Core/Application.UnitTests/UseCases/*`

**Inspecting the Database**
- VS Code extensions (SQLTools + SQLite driver, or alexcvzz.sqlite) or dedicated tools (DB Browser for SQLite, SQLiteStudio).
- The file `openversion.db` resides in the API’s working directory; you can change to a fixed path if preferred.

