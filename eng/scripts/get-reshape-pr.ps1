#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Install Reshape CLI from a specific Pull Request build.

.DESCRIPTION
    Downloads and installs the Reshape CLI from a GitHub Actions artifact
    built for a specific pull request using GitHub CLI (gh).

.PARAMETER PrNumber
    The pull request number to install from.

.PARAMETER OutputPath
    The directory where Reshape should be installed. Defaults to ~/.reshape

.EXAMPLE
    iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/get-reshape-pr.ps1) } 9"

.EXAMPLE
    ./get-reshape-pr.ps1 -PrNumber 9 -OutputPath "C:\Tools\Reshape"

.NOTES
    Requires GitHub CLI (gh) to be installed and authenticated
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [int]$PrNumber,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "$env:USERPROFILE\.reshape"
)

$ErrorActionPreference = 'Stop'

$repo = "jomaxso/Reshape"

Write-Host "🔄 Reshape PR Installer" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# Check for gh CLI
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "❌ GitHub CLI (gh) is required but not installed." -ForegroundColor Red
    Write-Host "   Install from: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

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
Write-Host "📦 Fetching PR #$PrNumber information..." -ForegroundColor Green

try {
    # Get PR head SHA
    $prInfo = gh api "repos/$repo/pulls/$PrNumber" --jq '{head_sha: .head.sha, state: .state}' | ConvertFrom-Json
    
    if ($prInfo.state -ne "open") {
        Write-Host "⚠️  Warning: PR #$PrNumber is $($prInfo.state)" -ForegroundColor Yellow
    }
    
    $headSha = $prInfo.head_sha
    Write-Host "   PR HEAD SHA: $headSha" -ForegroundColor Gray
    
    # Find workflow run for this PR
    Write-Host "🔍 Finding Build and Release workflow run..." -ForegroundColor Green
    
    $workflowRuns = gh api "repos/$repo/actions/workflows/build-and-release.yml/runs?event=pull_request&head_sha=$headSha" --jq '.workflow_runs | sort_by(.created_at) | reverse | .[0] | {id: .id, status: .status, conclusion: .conclusion, html_url: .html_url}' | ConvertFrom-Json
    
    if (-not $workflowRuns.id) {
        Write-Host "❌ No Build and Release workflow run found for this PR" -ForegroundColor Red
        Write-Host "   The workflow may not have started yet." -ForegroundColor Yellow
        Write-Host "   Check: https://github.com/$repo/pull/$PrNumber/checks" -ForegroundColor Yellow
        exit 1
    }
    
    $runId = $workflowRuns.id
    $runStatus = $workflowRuns.status
    $runConclusion = $workflowRuns.conclusion
    
    Write-Host "   Run ID: $runId" -ForegroundColor Gray
    Write-Host "   Status: $runStatus | Conclusion: $runConclusion" -ForegroundColor Gray
    
    if ($runStatus -ne "completed") {
        Write-Host "⏳ Workflow is still $runStatus. Please wait for it to complete." -ForegroundColor Yellow
        Write-Host "   Check: $($workflowRuns.html_url)" -ForegroundColor Yellow
        exit 1
    }
    
    if ($runConclusion -ne "success") {
        Write-Host "❌ Workflow run did not succeed (conclusion: $runConclusion)" -ForegroundColor Red
        Write-Host "   Check: $($workflowRuns.html_url)" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "✅ Found successful workflow run!" -ForegroundColor Green
    Write-Host ""
    
    # Determine artifact name based on OS
    $artifactName = "reshape-win-x64"
    
    Write-Host "📥 Downloading artifact: $artifactName" -ForegroundColor Green
    
    # Create temp directory
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "reshape-pr-$PrNumber-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    
    try {
        # Download artifact using gh CLI
        $downloadDir = Join-Path $tempDir "download"
        gh run download $runId --name $artifactName --dir $downloadDir --repo $repo
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to download artifact"
        }
        
        Write-Host "✅ Download complete!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📦 Installing to: $OutputPath" -ForegroundColor Green
        
        # Create output directory
        if (-not (Test-Path $OutputPath)) {
            New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        }
        
        # Find and extract archive
        $archive = Get-ChildItem -Path $downloadDir -Filter "*.zip" -Recurse | Select-Object -First 1
        
        if (-not $archive) {
            throw "No archive found in downloaded artifact"
        }
        
        # Extract
        Expand-Archive -Path $archive.FullName -DestinationPath $OutputPath -Force
        
        Write-Host "✅ Installation complete!" -ForegroundColor Green
        Write-Host ""
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "🎉 Reshape CLI from PR #$PrNumber is ready!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Run the CLI:" -ForegroundColor Cyan
        Write-Host "   $OutputPath\reshape.exe run" -ForegroundColor White
        Write-Host ""
        Write-Host "Or add to PATH:" -ForegroundColor Cyan
        Write-Host "   `$env:PATH += `";$OutputPath`"" -ForegroundColor White
        Write-Host "   reshape run" -ForegroundColor White
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    }
    finally {
        # Cleanup
        if (Test-Path $tempDir) {
            Remove-Item $tempDir -Recurse -Force
        }
    }
}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
