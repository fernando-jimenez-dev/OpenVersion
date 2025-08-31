param(
    [string]$Migration
)

$ErrorActionPreference = 'Stop'

# Resolve project paths relative to repo root
$root = Resolve-Path "$PSScriptRoot/.."
$applicationProj = Join-Path $root "Source/Core/Application/Application.csproj"
$startupProj = Join-Path $root "Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj"

$context = "Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.OpenVersionContext"

Write-Host "Applying EF Core migrations..." -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($Migration)) {
  dotnet ef database update `
    -c $context `
    -p $applicationProj `
    -s $startupProj
} else {
  dotnet ef database update $Migration `
    -c $context `
    -p $applicationProj `
    -s $startupProj
}

Write-Host "Done." -ForegroundColor Green

