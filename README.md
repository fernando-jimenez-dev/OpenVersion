**OpenVersion**

OpenVersion is a small, focused service that computes the next version number for a project based on branch naming conventions and simple bumping rules. It follows Clean Cut Architecture (CCA) to keep use cases, infrastructure, and presentation concerns decoupled and testable.

**Highlights**
- **Purpose:** Compute semantic-like version numbers per branch (main/qa/feature/fix).
- **Architecture:** Clean Cut Architecture (CCA) with explicit use cases and interfaces.
- **Persistence:** PostgreSQL (Npgsql) with EF Core for data access; schema migrations via FluentMigrator (separate Migrator project).
- **API:** Minimal API with two endpoints: health check and compute-next-version.
- **Testing:** Unit tests and realistic E2E tests using a real Postgres database.

**Requirements**
- **.NET SDK:** 8.0+
- **PowerShell:** 5.1+ or PowerShell 7+
- PostgreSQL (local Docker or managed). Optional: psql or a GUI to inspect DB.

**Architecture (CCA)**
- **Core/Application:** Use cases, domain models, abstractions, and error types. No framework dependencies leak into use case logic.
- **Infrastructure:** EF Core repository and version-bumping rules live behind interfaces in Application.
- **Presentation:** Minimal API project wires dependencies and exposes endpoints.
- **Guiding principles:** screaming architecture, high cohesion, low coupling, explicit error modeling.

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
- Connection string comes from layered appsettings in the WebAPI project:
  - `appsettings.json` → `appsettings.{Environment}.json` → optional `secrets.{Environment}.json` (gitignored).
- Migrations are handled by the Migrator project (FluentMigrator), not EF CLI.
- Run locally:
  - Latest: `dotnet run -p Database/Migrator/Migrator.csproj -- --conn "Host=localhost;Port=5432;Database=openversion_dev;Username=dev;Password=dev;SSL Mode=Disable"`
  - Recreate: `dotnet run -p Database/Migrator/Migrator.csproj -- --conn "<cs>" --recreate`

**E2E Tests**
- Uses a unique Postgres database per test run (e.g., `openversion_e2e_{guid}`).
- The test factory runs the Migrator in-process to build the schema, then wires DbContext to that DB, and drops it on dispose.
- Base connection comes from `Tests/Presentation/WebAPI.Minimal.E2E/e2e.appsettings.json` (localhost/dev-only).

**Getting Started**
- Install prerequisites: .NET 8, PowerShell, PostgreSQL (Docker is fine).
- Set `ConnectionStrings:OpenVersion` in WebAPI `appsettings.Development.json` or `secrets.Development.json`.
- Migrate (optional for dev): use the Migrator (see above) or rely on E2E to validate schema.
- Run the API: `dotnet run --project Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj`.
- Swagger (dev): `http://localhost:<port>/swagger`.

**Testing**
- Run unit tests: `dotnet test -v minimal`
- E2E tests: `dotnet test Tests/Presentation/WebAPI.Minimal.E2E/WebAPI.Minimal.E2E.csproj -v minimal`

**Inspecting the Database**
- Use psql or a Postgres GUI (e.g., pgAdmin, TablePlus). Ensure your Neon/local connection details match `appsettings.json`.
