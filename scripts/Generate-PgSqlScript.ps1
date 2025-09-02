param(
    [string]$From,
    [string]$To,
    [string]$Output
)

$ErrorActionPreference = 'Stop'

# Resolve project paths relative to repo root
$root = Resolve-Path "$PSScriptRoot/.."
$applicationProj = Join-Path $root "Source/Core/Application/Application.csproj"
$startupProj = Join-Path $root "Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj"

$context = "Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.OpenVersionContext"

# Default output path: scripts/migrations/yyyyMMddHHmmss-pg.sql
if ([string]::IsNullOrWhiteSpace($Output)) {
  $migrationsDir = Join-Path $root "scripts/migrations"
  if (-not (Test-Path $migrationsDir)) { New-Item -ItemType Directory -Path $migrationsDir | Out-Null }
  $timestamp = Get-Date -Format "yyyyMMddHHmmss"
  $Output = Join-Path $migrationsDir "$timestamp-pg.sql"
}

Write-Host "Generating idempotent PostgreSQL migration script..." -ForegroundColor Cyan

$args = @(
  'migrations', 'script',
  '-c', $context,
  '-p', $applicationProj,
  '-s', $startupProj,
  '--idempotent',
  '-o', $Output
)

if ($From) { $args += @('--from', $From) }
if ($To)   { $args += @('--to',   $To) }

dotnet ef @args

Write-Host "Script written to: $Output" -ForegroundColor Green
