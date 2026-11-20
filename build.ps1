param(
    [string]$Configuration = "Release",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "OutfitTool.sln"

if (-not $NoRestore) {
    dotnet restore $solution
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }
}

# -m:1 делает сборку предсказуемой в средах, где compiler server не может открыть named pipe.
dotnet build $solution -c $Configuration --no-restore -m:1
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

Write-Host "Build completed: $solution ($Configuration)" -ForegroundColor Green
