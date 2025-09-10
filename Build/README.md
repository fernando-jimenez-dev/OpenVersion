# Build Runner

This folder contains a Cake Frosting build runner with composable tasks for cleaning, compiling, testing, publishing, packaging, and deploying (IIS or Docker).

Run the runner
- PowerShell: `./build.ps1 --target <Target> [args]`
- Dotnet: `dotnet run --project Build/Runner/Runner.csproj -- --target <Target> [args]`

## Targets

Default
- Purpose: Compile the solution (Clean → Restore → Compile).
- Depends: Compile
- Args
  - `--configuration`: Build config (Debug|Release). Default: Debug

Clean
- Purpose: Clean solution outputs and `artifacts/` directory.
- Depends: —
- Args
  - `--configuration`: Build config (Debug|Release). Default: Debug

Restore
- Purpose: Restore NuGet packages for the solution.
- Depends: Clean
- Args: —

Compile
- Purpose: Build the solution.
- Depends: Restore
- Args
  - `--configuration`: Build config (Debug|Release). Default: Debug

Test
- Purpose: Orchestrate tests (unit and E2E). Subtasks self-skip based on flags.
- Depends: Test.Unit, Test.E2E
- Args
  - `--unit`: Run unit tests (true|false). Default: true
  - `--e2e`: Run E2E tests (true|false). Default: false
  - `--all`: Run both unit and E2E (true|false). Default: false

Test.Unit
- Purpose: Run unit tests only.
- Depends: Compile
- Args
  - `--unit` or `--all` must be true to execute
  - `--configuration`: Build config

Test.E2E
- Purpose: Run end-to-end tests only.
- Depends: Compile
- Args
  - `--e2e` or `--all` must be true to execute
  - `--configuration`: Build config

Publish.Web
- Purpose: `dotnet publish` WebAPI to `artifacts/publish/WebAPI.Minimal/<Configuration>[/<Runtime>]`.
- Depends: Compile
- Args
  - `--configuration`: Build config
  - `--runtime`: Runtime identifier (e.g., win-x64, linux-x64) (optional)

Package.Web
- Purpose: Zip publish output to `artifacts/packages/WebAPI.Minimal/WebAPI.Minimal-<Configuration>[-<Runtime>].zip`.
- Depends: Publish.Web
- Args
  - `--package`: Create zip (true|false). Default: false (when false, this task is a no-op)
  - `--configuration`: Build config
  - `--runtime`: Runtime identifier (optional)

Publish
- Purpose: Orchestrate Publish.Web and optional Package.Web.
- Depends: Publish.Web, Package.Web
- Args
  - `--package`: Create zip (true|false). Default: false

Deploy.IIS
- Purpose: Deploy publish output to a local IIS site/app pool.
- Depends: Secrets.MaterializeToPublish (which depends on Publish.Web)
- Args
  - `--configuration`: Build config
  - `--runtime`: Runtime identifier (optional)
  - `--siteName`: IIS site name. Default: OpenVersion
  - `--appPool`: IIS app pool name. Default: OpenVersion_AppPool
  - `--sitePath`: Physical path. Default: C:/inetpub/wwwroot/OpenVersion
  - `--port`: HTTP port. Default: 8080
  - `--secretsValuesPath`: Path to TOKEN→value JSON map used to materialize secrets.json (required)
- Notes: Requires Administrator, IIS WebAdministration module, and ASP.NET Core Hosting Bundle

Secrets.Materialize
- Purpose: Generate a plaintext `secrets.json` (or custom name) from a committed template by replacing `${TOKENS}` using a values file.
- Depends: —
- Args
  - `--secretsTemplatePath`: Template path. Default: `Source/Presentation/WebAPI.Minimal/secrets.template.json`
  - `--secretsOutDir`: Output directory. Default: `--sitePath` (IIS site folder)
  - `--secretsFileName`: Output filename. Default: `secrets.json`
  - `--secretsValuesPath`: Path to flat JSON object mapping TOKEN names to string values (required)

Secrets.MaterializeToPublish
- Purpose: Generate `secrets.json` into the Publish.Web output so Deploy.IIS copies it.
- Depends: Publish.Web
- Args
  - `--secretsTemplatePath`: Template path (default as above)
  - `--secretsFileName`: Output filename. Default: `secrets.json`
  - `--secretsValuesPath`: Path to flat JSON object mapping TOKEN→value (required)
- Notes: relative paths for template and values resolve from repo root when not absolute.

Deploy
- Purpose: Alias of Deploy.IIS.
- Depends: Deploy.IIS
- Args: same as Deploy.IIS

Deploy.Safe
- Purpose: Run tests first, then Deploy.IIS.
- Depends: Test, Deploy.IIS
- Args
  - Test flags: `--unit`, `--e2e`, `--all`
  - All Deploy.IIS args

Docker.Build
- Purpose: Build Docker image from repo Dockerfile (`Source/Presentation/WebAPI.Minimal/Dockerfile`).
- Depends: —
- Args
  - `--dockerImage`: Image name. Default: openversion-web
  - `--dockerTag`: Image tag. Default: local

Docker.Run
- Purpose: Run the WebAPI container.
- Depends: Docker.Build
- Args
  - `--dockerName`: Container name. Default: openversion-web
  - `--dockerPort`: Host port mapped to container 8080. Default: 8080
  - `--dockerEnv`: ASP.NET Core environment. Default: Development
  - `--dockerSecretsPath`: Path to secrets.{Env}.json on host; mounted read‑only to `/app/secrets.{Env}.json` (optional)
  - Uses `--dockerImage` and `--dockerTag` for the image reference

Deploy.Docker
- Purpose: Orchestrate Docker.Build → Docker.Run.
- Depends: Docker.Run
- Args: all Docker.* args

## Examples
- Compile: `./build.ps1 --target Compile --configuration Release`
- Unit + E2E tests: `./build.ps1 --target Test --all true`
- Publish + package: `./build.ps1 --target Publish --configuration Release --package true`
- Deploy to IIS on port 80 with values file:
  - `./build.ps1 --target Deploy.IIS --configuration Release --port 80 --secretsValuesPath "secrets/openversion.webapi.secrets.testing.json"`
- Docker deploy on port 8081 with Development secrets:
  - `./build.ps1 --target Deploy.Docker --dockerPort 8081 --dockerEnv Development --dockerSecretsPath "C:/path/secrets.Development.json"`

## Database migration tasks

Migrate.Db
- Purpose: Build a PostgreSQL connection string from individual args and run Database/Migrator.
- Args
  - `--dbHost`: e.g., mypg.postgres.database.azure.com
  - `--dbPort`: e.g., 5432
  - `--dbName`: database name
  - `--dbUser`: username
  - `--dbPassword`: password
  - `--dbSslMode`: Disable|Require|VerifyCA|VerifyFull
  - `--dbRecreate`: optional (true|false). Default: false
  - `--dbTarget`: optional long. Migrate up to specific version
  - `--migratorPath`: optional path to a published Migrator (directory or Migrator.dll). If omitted, runs from the csproj.

Publish.Migrator
- Purpose: `dotnet publish` Database/Migrator to `artifacts/publish/Migrator/<Configuration>`.
- Args
  - `--configuration`: Build config

Package.Migrator
- Purpose: Zip published migrator to `artifacts/packages/Migrator/Migrator-<Configuration>.zip`.
- Depends: Publish.Migrator
- Args
  - `--package`: Create zip (true|false). Default: false

Publish.All
- Purpose: Publish Web and Migrator; optionally package both when `--package=true`.
- Depends: Publish, Publish.Migrator, Package.Migrator
- Args
  - `--package`: Create zips for both Web and Migrator

Examples
- `./build.ps1 --target Migrate.Db --dbHost "$env:PG_HOST" --dbPort "$env:PG_PORT" --dbName "$env:PG_DB" --dbUser "$env:PG_USER" --dbPassword "$env:PG_PASSWORD" --dbSslMode "$env:PG_SSLMODE"`
- Run DB migrations using a published DLL:
  - `./build.ps1 --target Migrate.Db --dbHost ... --dbPort 5432 --dbName ... --dbUser ... --dbPassword ... --dbSslMode Require --migratorPath "$(System.DefaultWorkingDirectory)/_YourBuild/drop/Migrator-Release"`
- Produce both app + migrator zips:
  - `./build.ps1 --target Publish.All --configuration Release --package true`
