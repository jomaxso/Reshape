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
- Checkout code (with full git history)
- Setup .NET 10
- Install Nerdbank.GitVersioning tool
- Set PR build variables (for pull requests)
- Get version information
- Download UI artifact
- Restore dependencies
- Publish with AOT compilation
- Create platform-specific archive (.zip or .tar.gz)
- Generate SHA256 checksum
- Upload build artifact
```

Each platform build:
- Uses Native AOT compilation for fast startup
- Uses Nerdbank.GitVersioning for automatic version stamping
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
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/install.ps1) }"

# Specific version
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/install.ps1) } -Version v0.1.0"

# Custom install directory
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/install.ps1) } -InstallDir C:\Tools\Reshape"
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
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/install.sh | bash

# Specific version
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/install.sh | bash -s -- --version v0.1.0

# Custom install directory
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/scripts/install.sh | bash -s -- --install-dir ~/bin
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

## Workflow Monitoring

Reshape uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) for automated semantic versioning. The version is automatically derived from git history and follows [Semantic Versioning 2.0](https://semver.org/).

#### Version Format

**Stable Releases (from main or version tags):**
```
MAJOR.MINOR.PATCH
```

**Preview/PR Builds:**
```
MAJOR.MINOR.PATCH-preview.{height}+{commit-id}
```

Where:
- **MAJOR.MINOR.PATCH**: Base version from `version.json`
- **preview**: Prerelease tag for non-public builds
- **{height}**: Number of commits since the last version change
- **{commit-id}**: Short git commit hash

#### Configuration

Versioning is configured in `/version.json`:

```json
{
  "version": "0.1",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/tags/v\\d+\\.\\d+\\.\\d+$"
  ],
  "release": {
    "firstUnstableTag": "preview"
  }
}
```

**Public Releases** (no preview tag):
- Commits on the `main` branch
- Version tags matching `vX.Y.Z` pattern

**Preview Releases** (with preview tag):
- Pull request builds
- Feature branch builds
- Any commit not matching `publicReleaseRefSpec`

#### How Versioning Works

1. **Git-Based Calculation**: Nerdbank.GitVersioning analyzes git history to calculate the version
2. **Automatic Application**: Version is automatically applied during build (no manual version updates needed in `.csproj`)
3. **Build Metadata**: Non-public releases include git commit hash as build metadata
4. **Height Calculation**: The height value increments with each commit, providing a unique version for every build

#### Updating Version

To change the base version (e.g., from 0.1 to 0.2):

1. Edit `version.json`:
   ```json
   {
     "version": "0.2",
     ...
   }
   ```

2. Commit the change:
   ```bash
   git add version.json
   git commit -m "Bump version to 0.2"
   git push
   ```

The version is automatically applied to all subsequent builds.

#### CI/CD Integration

GitHub Actions workflows automatically use Nerdbank.GitVersioning:

```yaml
- name: Install Nerdbank.GitVersioning
  run: dotnet tool install -g nbgv

- name: Get version
  id: nbgv
  run: |
    VERSION=$(nbgv get-version -v Version)
    echo "version=${VERSION}" >> $GITHUB_OUTPUT
```

For PR builds, the workflow includes additional steps to track PR numbers in build logs.

### Semantic Versioning Guidelines

Follow these guidelines when bumping versions:

- **Major** (X.0.0): Breaking changes, incompatible API changes
- **Minor** (0.X.0): New features, backward compatible
- **Patch** (0.0.X): Bug fixes, backward compatible (handled automatically by git height)

### Prerelease Versions

For alpha/beta releases, use the version field in `version.json`:

```json
{
  "version": "1.0-alpha",
  ...
}
```

or

```json
{
  "version": "1.0-beta",
  ...
}
```

This will generate versions like:
- `1.0.0-alpha.5+abc1234` (on feature branches)
- `1.0.0-alpha.5` (on main)

## Creating a Release

### Automated Release Process

1. **Update version** (if needed) in `version.json`

2. **Commit all changes** to main:
   ```bash
   git add .
   git commit -m "Release preparation"
   git push origin main
   ```

3. **Use the GitHub Release Workflow**:
   - Go to Actions → "Create Stable Release"
   - Click "Run workflow"
   - Enter version (or leave empty to use version from nbgv)
   - Click "Run workflow"

The workflow will:
- Calculate or use the specified version
- Create a git tag
- Trigger the build-and-release workflow
- Build binaries for all platforms
- Create a GitHub release with artifacts

### Manual Release Creation

Alternatively, create and push a version tag:

```bash
# Get current version
nbgv get-version

# Create tag
git tag -a v0.1.0 -m "Release version 0.1.0"
git push origin v0.1.0
```

### Prerelease Workflow

The prerelease workflow automatically runs on every push to `main`:

- Creates a rolling "prerelease" tag
- Builds and uploads binaries
- Marks as prerelease in GitHub
- Accessible via `--prerelease` flag in install scripts

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
