#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates the version in eng/Versions.props
    
.DESCRIPTION
    This script helps developers update version numbers in eng/Versions.props.
    It can increment major, minor, or patch versions, and set prerelease labels.
    
.PARAMETER Major
    Increment the major version
    
.PARAMETER Minor
    Increment the minor version
    
.PARAMETER Patch
    Increment the patch version
    
.PARAMETER SetVersion
    Set a specific version (format: MAJOR.MINOR.PATCH)
    
.PARAMETER Prerelease
    Set the prerelease label (e.g., 'preview.1', 'rc.1')
    Leave empty to clear the prerelease label
    
.PARAMETER Show
    Display the current version without making changes
    
.EXAMPLE
    .\scripts\set-version.ps1 -Patch
    Increments the patch version (e.g., 0.1.0 -> 0.1.1)
    
.EXAMPLE
    .\scripts\set-version.ps1 -Minor -Prerelease "preview.1"
    Increments the minor version and sets prerelease label (e.g., 0.1.0 -> 0.2.0-preview.1)
    
.EXAMPLE
    .\scripts\set-version.ps1 -SetVersion "1.0.0"
    Sets the version to 1.0.0
    
.EXAMPLE
    .\scripts\set-version.ps1 -Show
    Displays the current version
#>

param(
    [switch]$Major,
    [switch]$Minor,
    [switch]$Patch,
    [string]$SetVersion,
    [string]$Prerelease,
    [switch]$Show
)

$ErrorActionPreference = "Stop"

$VersionsPropsPath = Join-Path $PSScriptRoot ".." "eng" "Versions.props"

if (-not (Test-Path $VersionsPropsPath)) {
    Write-Error "eng/Versions.props not found at: $VersionsPropsPath"
    exit 1
}

function Get-XmlValue {
    param([xml]$Xml, [string]$ElementName)
    $element = $Xml.SelectSingleNode("//PropertyGroup/$ElementName")
    if ($element) {
        return $element.InnerText
    }
    return ""
}

function Set-XmlValue {
    param([xml]$Xml, [string]$ElementName, [string]$Value)
    $element = $Xml.SelectSingleNode("//PropertyGroup/$ElementName")
    if ($element) {
        $element.InnerText = $Value
    }
}

# Load XML
[xml]$xml = Get-Content $VersionsPropsPath

# Get current version
$currentMajor = Get-XmlValue $xml "MajorVersion"
$currentMinor = Get-XmlValue $xml "MinorVersion"
$currentPatch = Get-XmlValue $xml "PatchVersion"
$currentPrerelease = Get-XmlValue $xml "PreReleaseVersionLabel"

$currentVersion = "$currentMajor.$currentMinor.$currentPatch"
if ($currentPrerelease) {
    $currentVersion += "-$currentPrerelease"
}

if ($Show) {
    Write-Host ""
    Write-Host "Current Version Information:" -ForegroundColor Cyan
    Write-Host "  Version:    $currentVersion" -ForegroundColor Green
    Write-Host "  Major:      $currentMajor" -ForegroundColor Gray
    Write-Host "  Minor:      $currentMinor" -ForegroundColor Gray
    Write-Host "  Patch:      $currentPatch" -ForegroundColor Gray
    if ($currentPrerelease) {
        Write-Host "  Prerelease: $currentPrerelease" -ForegroundColor Gray
    }
    Write-Host ""
    exit 0
}

# Calculate new version
$newMajor = [int]$currentMajor
$newMinor = [int]$currentMinor
$newPatch = [int]$currentPatch

if ($SetVersion) {
    # Parse the provided version
    if ($SetVersion -match '^(\d+)\.(\d+)\.(\d+)$') {
        $newMajor = [int]$Matches[1]
        $newMinor = [int]$Matches[2]
        $newPatch = [int]$Matches[3]
    }
    else {
        Write-Error "Invalid version format. Use MAJOR.MINOR.PATCH (e.g., 1.0.0)"
        exit 1
    }
}
elseif ($Major) {
    $newMajor++
    $newMinor = 0
    $newPatch = 0
}
elseif ($Minor) {
    $newMinor++
    $newPatch = 0
}
elseif ($Patch) {
    $newPatch++
}
else {
    Write-Error "No operation specified. Use -Major, -Minor, -Patch, -SetVersion, or -Show"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\scripts\set-version.ps1 -Patch" -ForegroundColor Gray
    Write-Host "  .\scripts\set-version.ps1 -Minor -Prerelease 'preview.1'" -ForegroundColor Gray
    Write-Host "  .\scripts\set-version.ps1 -SetVersion '1.0.0'" -ForegroundColor Gray
    Write-Host "  .\scripts\set-version.ps1 -Show" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

# Update XML
Set-XmlValue $xml "MajorVersion" $newMajor
Set-XmlValue $xml "MinorVersion" $newMinor
Set-XmlValue $xml "PatchVersion" $newPatch

if ($PSBoundParameters.ContainsKey('Prerelease')) {
    Set-XmlValue $xml "PreReleaseVersionLabel" $Prerelease
    $newPrerelease = $Prerelease
}
else {
    $newPrerelease = $currentPrerelease
}

# Save XML
$xml.Save($VersionsPropsPath)

$newVersion = "$newMajor.$newMinor.$newPatch"
if ($newPrerelease) {
    $newVersion += "-$newPrerelease"
}

Write-Host ""
Write-Host "✅ Version Updated Successfully" -ForegroundColor Green
Write-Host ""
Write-Host "  Previous: $currentVersion" -ForegroundColor Red
Write-Host "  New:      $newVersion" -ForegroundColor Green
Write-Host ""
Write-Host "Changes saved to: $VersionsPropsPath" -ForegroundColor Gray
Write-Host ""
