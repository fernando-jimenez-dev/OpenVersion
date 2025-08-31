param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$ErrorActionPreference = 'Stop'

# Resolve project paths relative to repo root
$root = Resolve-Path "$PSScriptRoot/.."
$applicationProj = Join-Path $root "Source/Core/Application/Application.csproj"
$startupProj = Join-Path $root "Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj"

$context = "Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.OpenVersionContext"
$migrationsOutput = "UseCases/ComputeNextVersion/Infrastructure/EntityFramework/Migrations"

Write-Host "Adding EF Core migration '$Name'..." -ForegroundColor Cyan
dotnet ef migrations add $Name `
  -c $context `
  -p $applicationProj `
  -s $startupProj `
  -o $migrationsOutput

Write-Host "Done." -ForegroundColor Green

