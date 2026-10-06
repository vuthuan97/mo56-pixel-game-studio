# Build + test script for Pixel Game Studio (Windows PowerShell).
# Usage: ./scripts/build.ps1 [-Test] [-Format]
param(
    [switch]$Test,
    [switch]$Format
)

$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

if ($Format) {
    dotnet format PixelGameStudio.sln
}

dotnet build PixelGameStudio.sln

if ($Test) {
    dotnet test PixelGameStudio.sln
}
