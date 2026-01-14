#!/usr/bin/env pwsh
# Reshape CLI Installation Script for Windows/PowerShell
# Usage: iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) }"
# Or with version: iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) } -Version v0.1.0"

param(
    [string]$Version = "latest",
    [string]$InstallDir = "$env:USERPROFILE\.reshape\bin"
)

$ErrorActionPreference = "Stop"

# Color output functions
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Info {
    Write-ColorOutput Cyan $args
}

function Write-Success {
    Write-ColorOutput Green $args
}

function Write-Warning {
    Write-ColorOutput Yellow $args
}

function Write-Error {
    Write-ColorOutput Red $args
}

Write-Info "🔄 Reshape CLI Installer"
Write-Info "========================"

# Determine version to install
$repo = "jomaxso/Reshape"
$apiUrl = "https://api.github.com/repos/$repo/releases"

try {
    if ($Version -eq "latest") {
        Write-Info "Fetching latest release information..."
        $release = Invoke-RestMethod -Uri "$apiUrl/latest" -Headers @{"User-Agent"="Reshape-Installer"}
        $Version = $release.tag_name
    } else {
        Write-Info "Fetching release information for $Version..."
        $release = Invoke-RestMethod -Uri "$apiUrl/tags/$Version" -Headers @{"User-Agent"="Reshape-Installer"}
    }
} catch {
    Write-Error "Failed to fetch release information: $_"
    exit 1
}

Write-Success "Installing Reshape CLI version: $Version"

# Determine architecture
$arch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
$assetName = "reshape-win-$arch.zip"

# Find the asset
$asset = $release.assets | Where-Object { $_.name -eq $assetName }
if (-not $asset) {
    Write-Error "Could not find asset $assetName in release $Version"
    Write-Info "Available assets:"
    $release.assets | ForEach-Object { Write-Info "  - $($_.name)" }
    exit 1
}

# Create temporary directory
$tempDir = Join-Path $env:TEMP "reshape-install-$(Get-Random)"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    # Download the archive
    $downloadUrl = $asset.browser_download_url
    $archivePath = Join-Path $tempDir $assetName
    
    Write-Info "Downloading from: $downloadUrl"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath -Headers @{"User-Agent"="Reshape-Installer"}
    Write-Success "Downloaded successfully"

    # Extract archive
    Write-Info "Extracting archive..."
    Expand-Archive -Path $archivePath -DestinationPath $tempDir -Force

    # Create installation directory
    if (-not (Test-Path $InstallDir)) {
        Write-Info "Creating installation directory: $InstallDir"
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }

    # Find the executable in the extracted folder
    $exeName = "Reshape.Cli.exe"
    $exePath = Get-ChildItem -Path $tempDir -Recurse -Filter $exeName | Select-Object -First 1

    if (-not $exePath) {
        Write-Error "Could not find $exeName in the downloaded archive"
        exit 1
    }

    # Copy executable to installation directory
    $targetPath = Join-Path $InstallDir "reshape.exe"
    Write-Info "Installing to: $targetPath"
    
    # Handle case where file might be in use
    if (Test-Path $targetPath) {
        try {
            Remove-Item $targetPath -Force
        } catch {
            Write-Warning "Could not replace existing file (it may be in use)"
            $tempTarget = Join-Path $InstallDir "reshape.new.exe"
            Copy-Item $exePath.FullName $tempTarget -Force
            Write-Warning "Installed as: $tempTarget"
            Write-Warning "Please close any running Reshape instances and rename manually"
            Write-Warning "  From: $tempTarget"
            Write-Warning "  To:   $targetPath"
            exit 0
        }
    }

    Copy-Item $exePath.FullName $targetPath -Force
    Write-Success "✓ Reshape CLI installed successfully!"

    # Check if directory is in PATH
    $pathDirs = $env:PATH -split ';'
    $isInPath = $pathDirs -contains $InstallDir

    if (-not $isInPath) {
        Write-Warning "`nⓘ Installation directory is not in your PATH"
        Write-Info "To add it to your PATH, run:"
        Write-Info '  $env:PATH += ";$InstallDir"'
        Write-Info "Or add it permanently via System Environment Variables"
        Write-Info ""
        Write-Info "To use reshape right now, run:"
        Write-Info "  & '$targetPath' --help"
    } else {
        Write-Success "`n✓ You can now use the 'reshape' command"
        Write-Info "Try: reshape --help"
    }

} finally {
    # Cleanup
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force
    }
}

Write-Info "`nFor more information, visit: https://github.com/$repo"
