# CI/CD Pipeline & Release Flow

This document describes the CI/CD pipeline, release workflow, and installation system for Reshape CLI.

## Overview

Reshape uses GitHub Actions for automated building, testing, and releasing of binaries across multiple platforms. The system consists of:

1. **GitHub Actions Workflow** - Automated build and release pipeline
2. **Installation Scripts** - Platform-specific installation scripts
3. **Self-Update System** - Built-in update capability via the `reshape update` command

## GitHub Actions Workflow

The workflow is defined in `.github/workflows/build-and-release.yml` and handles:

### Triggers

- **Tag Push**: Automatically creates a release when a version tag (e.g., `v0.1.0`) is pushed
- **Pull Request**: Runs build and tests on PRs to `main` branch
- **Manual Dispatch**: Can be triggered manually from the Actions tab

### Build Matrix

The workflow builds for the following platforms:

| Platform | Architecture | Runtime ID | Artifact Name |
|----------|-------------|------------|---------------|
| Windows | x64 | win-x64 | reshape-win-x64.zip |
| Linux | x64 | linux-x64 | reshape-linux-x64.tar.gz |
| macOS | ARM64 | osx-arm64 | reshape-osx-arm64.tar.gz |

### Workflow Stages

#### 1. Build Vue UI

```yaml
- Checkout code
- Setup Node.js 20.x
- Install dependencies (npm ci)
- Build UI (npm run build)
- Upload UI artifact
```

The Vue UI is built once and reused for all platform builds.

#### 2. Build CLI (per platform)

```yaml
- Checkout code
- Setup .NET 10
- Download UI artifact
- Restore dependencies
- Publish with AOT compilation
- Create platform-specific archive (.zip or .tar.gz)
- Generate SHA256 checksum
- Upload build artifact
```

Each platform build:
- Uses Native AOT compilation for fast startup
- Creates a self-contained executable
- Generates SHA256 checksum for verification

#### 3. Create Release (tags only)

```yaml
- Download all build artifacts
- Copy installation scripts
- Create GitHub Release
- Upload all artifacts and scripts
```

Releases include:
- Platform binaries (zipped/tarred)
- SHA256 checksums
- Installation scripts (`install.ps1`, `install.sh`)
- Auto-generated release notes

## Installation Scripts

### PowerShell Script (`install.ps1`)

For Windows users. Features:

- Downloads the latest release (or specific version)
- Extracts to `~/.reshape/bin`
- Handles locked files (running instances)
- Provides PATH setup instructions

**Usage:**

```powershell
# Latest version
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) }"

# Specific version
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) } -Version v0.1.0"

# Custom install directory
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) } -InstallDir C:\Tools\Reshape"
```

### Bash Script (`install.sh`)

For Linux and macOS users. Features:

- Auto-detects OS and architecture
- Downloads the correct binary
- Installs to `~/.local/bin`
- Sets executable permissions
- Provides PATH setup instructions

**Usage:**

```bash
# Latest version
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash

# Specific version
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash -s -- --version v0.1.0

# Custom install directory
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash -s -- --install-dir ~/bin
```

## Self-Update System

The `reshape update` command provides built-in update capability.

### How It Works

1. **Check Version**: Queries GitHub API for latest release
2. **Compare**: Compares local version with latest release
3. **Download**: Downloads the appropriate binary for your platform
4. **Replace**: Replaces the current executable
5. **Cleanup**: Removes temporary files

### Update Command Options

```bash
# Check for updates without installing
reshape update --check

# Update to latest stable release
reshape update

# Include prerelease versions
reshape update --prerelease
```

### Platform-Specific Handling

#### Windows

On Windows, the running executable cannot be replaced directly. The update command:

1. Attempts direct replacement
2. If locked, creates a batch script to replace after exit
3. Notifies the user the update will complete after closing

#### Linux/macOS

On Unix systems:
1. Downloads and extracts the update
2. Replaces the executable directly
3. Sets executable permissions
4. Update completes immediately

## Creating a Release

### Prerequisites

1. Update version in `src/reshape-cli/Reshape.Cli.csproj`:
   ```xml
   <Version>X.Y.Z</Version>
   ```

2. Commit all changes

3. Create and push a version tag:
   ```bash
   git tag -a vX.Y.Z -m "Release version X.Y.Z"
   git push origin vX.Y.Z
   ```

### Versioning Scheme

Follow [Semantic Versioning](https://semver.org/):

- **Major** (X.0.0): Breaking changes
- **Minor** (0.X.0): New features, backward compatible
- **Patch** (0.0.X): Bug fixes, backward compatible

### Prerelease Versions

For prerelease versions, use suffixes:

- Alpha: `v0.1.0-alpha.1`
- Beta: `v0.1.0-beta.1`
- Release Candidate: `v0.1.0-rc.1`

Prereleases are automatically marked as such in GitHub Releases.

## Workflow Monitoring

### View Workflow Runs

1. Go to the [Actions tab](https://github.com/jomaxso/Reshape/actions)
2. Select "Build and Release" workflow
3. View run details, logs, and artifacts

### Common Issues

#### Build Fails

- Check .NET 10 SDK compatibility
- Verify all NuGet packages restore correctly
- Check for compilation errors in logs

#### UI Build Fails

- Verify Node.js 20+ is available
- Check `package-lock.json` is committed
- Review npm install logs

#### AOT Compilation Fails

- Ensure all JSON types are in `AppJsonSerializerContext.cs`
- Avoid reflection-based APIs
- Check for platform-specific issues

## Testing the Pipeline

### Test Build Without Release

Push to a branch and create a PR to `main`:

```bash
git checkout -b test-build
git push origin test-build
# Create PR on GitHub
```

This will trigger the build workflow without creating a release.

### Test Release Creation

Create a test tag:

```bash
git tag -a v0.0.0-test.1 -m "Test release"
git push origin v0.0.0-test.1
```

This creates a prerelease that can be deleted after testing.

## Security

### Checksums

Each release includes SHA256 checksums for verification:

```bash
# Download checksum file
curl -LO https://github.com/jomaxso/Reshape/releases/download/v0.1.0/reshape-linux-x64.tar.gz.sha256

# Verify (Linux/macOS)
shasum -a 256 -c reshape-linux-x64.tar.gz.sha256

# Verify (Windows PowerShell)
$expected = Get-Content reshape-win-x64.zip.sha256 | Select-Object -First 1 | ForEach-Object { $_.Split()[0] }
$actual = (Get-FileHash -Algorithm SHA256 reshape-win-x64.zip).Hash.ToLower()
$expected -eq $actual
```

### Signed Releases

Future enhancement: Code signing for executables
- Windows: Authenticode signing
- macOS: Apple code signing
- Linux: GPG signatures

## Troubleshooting

### Installation Script Fails

**Windows:**
- Ensure PowerShell execution policy allows scripts
- Check internet connectivity to GitHub
- Verify `iwr` (Invoke-WebRequest) is available

**Linux/macOS:**
- Check `curl` is installed
- Verify permissions for install directory
- Ensure `tar` is available

### Update Command Fails

- Check internet connectivity
- Verify GitHub API is accessible
- Check for sufficient disk space
- On Windows, close all running instances

### Manual Installation

If scripts fail, download manually:

1. Go to [Releases](https://github.com/jomaxso/Reshape/releases)
2. Download the appropriate file
3. Extract the archive
4. Move executable to a directory in PATH

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Publishing Documentation](https://docs.microsoft.com/en-us/dotnet/core/deploying/)
- [Semantic Versioning](https://semver.org/)
