#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Install Reshape CLI from a specific Pull Request build.

.DESCRIPTION
    Downloads and installs the Reshape CLI from a GitHub Actions artifact
    built for a specific pull request.

.PARAMETER PrNumber
    The pull request number to install from.

.PARAMETER OutputPath
    The directory where Reshape should be installed. Defaults to ~/.reshape

.EXAMPLE
    iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/get-reshape-pr.ps1) } 9"

.EXAMPLE
    ./get-reshape-pr.ps1 -PrNumber 9 -OutputPath "C:\Tools\Reshape"
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [int]$PrNumber,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "$env:USERPROFILE\.reshape"
)

$ErrorActionPreference = 'Stop'

$repo = "jomaxso/Reshape"
$artifactName = "reshape-win-x64"

Write-Host "🔄 Reshape PR Installer" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# Warning message
Write-Host "⚠️  WARNING: Only install from PRs you trust!" -ForegroundColor Yellow
Write-Host "   Review the code at: https://github.com/$repo/pull/$PrNumber" -ForegroundColor Yellow
Write-Host ""

# Ask for confirmation
$confirmation = Read-Host "Continue with installation? (y/N)"
if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
    Write-Host "❌ Installation cancelled." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📦 Fetching PR #$PrNumber artifacts..." -ForegroundColor Green

# Get workflow runs for this PR
$workflowsUrl = "https://api.github.com/repos/$repo/actions/runs?event=pull_request&status=success&per_page=100"

try {
    $headers = @{
        'Accept'     = 'application/vnd.github+json'
        'User-Agent' = 'Reshape-PR-Installer'
    }

    $runs = Invoke-RestMethod -Uri $workflowsUrl -Headers $headers
    
    # Find the most recent run for this PR
    $prRun = $runs.workflow_runs | Where-Object { 
        $_.pull_requests.number -contains $PrNumber 
    } | Select-Object -First 1

    if (-not $prRun) {
        Write-Host "❌ No successful workflow run found for PR #$PrNumber" -ForegroundColor Red
        Write-Host "   Check if the PR has completed builds: https://github.com/$repo/pull/$PrNumber/checks" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✅ Found workflow run: $($prRun.name) #$($prRun.run_number)" -ForegroundColor Green
    Write-Host "   Status: $($prRun.status) | Conclusion: $($prRun.conclusion)" -ForegroundColor Gray
    Write-Host ""

    # Get artifacts for this run
    $artifactsUrl = $prRun.artifacts_url
    $artifacts = Invoke-RestMethod -Uri $artifactsUrl -Headers $headers

    $artifact = $artifacts.artifacts | Where-Object { $_.name -eq $artifactName } | Select-Object -First 1

    if (-not $artifact) {
        Write-Host "❌ Artifact '$artifactName' not found" -ForegroundColor Red
        Write-Host "   Available artifacts:" -ForegroundColor Yellow
        $artifacts.artifacts | ForEach-Object { Write-Host "   - $($_.name)" -ForegroundColor Gray }
        exit 1
    }

    Write-Host "📥 Downloading artifact: $($artifact.name)" -ForegroundColor Green
    Write-Host "   Size: $([math]::Round($artifact.size_in_bytes / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host ""

    # GitHub requires authentication to download artifacts via API
    # So we'll direct users to download manually or use gh CLI
    Write-Host "⚠️  GitHub requires authentication to download artifacts." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Download manually" -ForegroundColor Cyan
    Write-Host "  1. Go to: $($prRun.html_url)" -ForegroundColor White
    Write-Host "  2. Scroll to 'Artifacts'" -ForegroundColor White
    Write-Host "  3. Download '$artifactName.zip'" -ForegroundColor White
    Write-Host "  4. Extract to: $OutputPath" -ForegroundColor White
    Write-Host ""
    Write-Host "Option 2: Use GitHub CLI (gh)" -ForegroundColor Cyan
    Write-Host "  gh run download $($prRun.id) --name $artifactName --repo $repo" -ForegroundColor White
    Write-Host ""
    Write-Host "Direct link to workflow run:" -ForegroundColor Magenta
    Write-Host "  $($prRun.html_url)" -ForegroundColor Blue

}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "💡 After extracting, add to PATH:" -ForegroundColor Cyan
Write-Host "   `$env:PATH += `";$OutputPath`"" -ForegroundColor White
Write-Host ""
Write-Host "   Or run directly:" -ForegroundColor Cyan
Write-Host "   $OutputPath\reshape.exe run" -ForegroundColor White
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
