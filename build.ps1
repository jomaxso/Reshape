#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for Reshape CLI and UI

.DESCRIPTION
    Builds the Vue UI and .NET CLI in the correct order.
    This ensures the wwwroot folder is populated before publishing.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug

.PARAMETER Runtime
    Target runtime identifier (e.g., win-x64, linux-x64, osx-arm64)
    If specified, will publish a self-contained executable.

.PARAMETER SkipUI
    Skip building the UI (useful if UI hasn't changed)

.PARAMETER SkipRestore
    Skip dotnet restore (useful for faster rebuilds)

.EXAMPLE
    .\build.ps1
    Build for development (Debug)

.EXAMPLE
    .\build.ps1 -Configuration Release
    Build for release locally

.EXAMPLE
    .\build.ps1 -Configuration Release -Runtime win-x64
    Publish a Windows x64 executable
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    
    [string]$Runtime,
    
    [switch]$SkipUI,
    
    [switch]$SkipRestore
)

$ErrorActionPreference = 'Stop'

# Colors
function Write-Step { Write-Host "▶ $args" -ForegroundColor Cyan }
function Write-Success { Write-Host "✓ $args" -ForegroundColor Green }
function Write-Error { Write-Host "✗ $args" -ForegroundColor Red }

$rootDir = $PSScriptRoot
$uiDir = Join-Path $rootDir "src/Reshape.Ui"
$cliDir = Join-Path $rootDir "src/Reshape.Cli"
$wwwrootDir = Join-Path $cliDir "wwwroot"

try {
    # Step 1: Build UI
    if (-not $SkipUI) {
        Write-Step "Building Vue UI..."
        Push-Location $uiDir
        
        if (-not (Test-Path "node_modules")) {
            Write-Step "Installing npm dependencies..."
            npm install
        }
        
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "UI build failed" }
        
        Pop-Location
        Write-Success "UI build complete → $wwwrootDir"
    } else {
        Write-Host "Skipping UI build" -ForegroundColor Yellow
    }
    
    # Verify wwwroot exists
    if (-not (Test-Path $wwwrootDir)) {
        Write-Error "wwwroot folder not found! UI must be built first."
        exit 1
    }
    
    # Step 2: Restore .NET dependencies
    if (-not $SkipRestore) {
        Write-Step "Restoring .NET dependencies..."
        Push-Location $cliDir
        dotnet restore
        if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
        Pop-Location
    }
    
    # Step 3: Build or Publish .NET CLI
    Push-Location $cliDir
    
    if ($Runtime) {
        Write-Step "Publishing for $Runtime ($Configuration)..."
        $publishDir = Join-Path $cliDir "publish/$Runtime"
        dotnet publish -c $Configuration -r $Runtime --self-contained -o $publishDir
        if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
        
        Write-Success "Published to $publishDir"
        Write-Host ""
        Write-Host "Executable: " -NoNewline
        $exeName = if ($Runtime.StartsWith('win-')) { 'reshape.exe' } else { 'reshape' }
        Write-Host (Join-Path $publishDir $exeName) -ForegroundColor White
    } else {
        Write-Step "Building .NET CLI ($Configuration)..."
        dotnet build -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
        
        Write-Success "Build complete"
        Write-Host ""
        Write-Host "Run with: " -NoNewline
        Write-Host "dotnet run -- run" -ForegroundColor White
    }
    
    Pop-Location
    
    Write-Host ""
    Write-Success "All done!"
    
} catch {
    Pop-Location -ErrorAction SilentlyContinue
    Write-Error $_.Exception.Message
    exit 1
}
