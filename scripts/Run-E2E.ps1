# May be replaced once I have a Cake Frosting Solution for this

param()

$ErrorActionPreference = 'Stop'

$root = Resolve-Path "$PSScriptRoot/.."
$proj = Join-Path $root "Tests/Presentation/WebAPI.Minimal.E2E/WebAPI.Minimal.E2E.csproj"

Write-Host "Running E2E tests..." -ForegroundColor Cyan
dotnet test $proj -v minimal
Write-Host "Done." -ForegroundColor Green

