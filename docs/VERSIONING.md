# Reshape Versioning System

This document describes how versioning works in the Reshape CLI project.

## Overview

Reshape follows a centralized versioning approach similar to the [.NET Aspire project](https://github.com/dotnet/aspire). All version information is managed in a single location: **`eng/Versions.props`**.

## Version Structure

The version follows [Semantic Versioning 2.0.0](https://semver.org/) with the format:

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILDMETADATA]
```

Example versions:
- `0.1.0` - Stable release (manual via release.yml)
- `0.1.0-preview.1` - Preview release (automatic on push to main)
- `0.1.0-preview.1+abc1234` - Prerelease with build metadata (on PR to main)

## Version Configuration

### eng/Versions.props

All version components are defined in `eng/Versions.props`:

```xml
<PropertyGroup>
  <!-- Reshape Version -->
  <MajorVersion>0</MajorVersion>
  <MinorVersion>1</MinorVersion>
  <PatchVersion>0</PatchVersion>
  <VersionPrefix>$(MajorVersion).$(MinorVersion).$(PatchVersion)</VersionPrefix>
  
  <!-- Prerelease Label -->
  <PreReleaseVersionLabel></PreReleaseVersionLabel>
  
  <!-- Build Metadata -->
  <BuildMetadata Condition="'$(BuildMetadata)' == ''"></BuildMetadata>
</PropertyGroup>
```

### Version Components

- **MajorVersion**: Incremented for breaking changes
- **MinorVersion**: Incremented for new features (backwards compatible)
- **PatchVersion**: Incremented for bug fixes
- **PreReleaseVersionLabel**: Optional prerelease identifier (e.g., `preview.1`, `rc.1`)
- **BuildMetadata**: Optional build information (e.g., commit SHA)

## Release Types

### Stable Releases

For stable releases:
1. Set `MajorVersion`, `MinorVersion`, and `PatchVersion` appropriately
2. Leave `PreReleaseVersionLabel` empty
3. Create a release tag: `v0.1.0`

Example:
```xml
<MajorVersion>0</MajorVersion>
<MinorVersion>1</MinorVersion>
<PatchVersion>0</PatchVersion>
<PreReleaseVersionLabel></PreReleaseVersionLabel>
```

### Preview/RC Releases

For preview or release candidate versions:
1. Set version numbers as needed
2. Set `PreReleaseVersionLabel` (e.g., `preview.1`, `rc.2`)
3. Create a release tag: `v0.2.0-preview.1`

Example:
```xml
<MajorVersion>0</MajorVersion>
<MinorVersion>2</MinorVersion>
<PatchVersion>0</PatchVersion>
<PreReleaseVersionLabel>preview.1</PreReleaseVersionLabel>
```

### Preview Builds

Preview builds are created automatically on every push to `main`:

- **Preview Release**: Clean version with build number only
- Format: `0.1.0-preview.42`

### PR Prerelease Builds

During pull requests to `main`, builds include commit metadata:

- **Prerelease with Build Metadata**: Includes commit SHA for traceability
- Format: `0.1.0-preview.42+abc1234`

## Automated Version Updates

### Preview on Main Push

When changes are pushed to `main`, the `prerelease.yml` workflow:

1. Reads base version from `eng/Versions.props`
2. Appends build number: `-preview.{build_number}`
3. Creates/updates the `prerelease` tag and release

Example: `0.1.0-preview.42`

**Note**: During PR builds (before merge), the version includes commit SHA for traceability: `0.1.0-preview.42+abc1234`

## Manual Version Updates

To manually update the version:

1. Edit `eng/Versions.props`:
   ```xml
   <MajorVersion>1</MajorVersion>
   <MinorVersion>0</MinorVersion>
   <PatchVersion>0</PatchVersion>
   <PreReleaseVersionLabel>preview.1</PreReleaseVersionLabel>
   ```

2. Commit and push the changes

3. For stable releases, trigger the release workflow:
   ```bash
   gh workflow run release.yml
   ```

## Package Version Management

All NuGet package versions are centrally managed using [Central Package Management (CPM)](https://learn.microsoft.com/nuget/consume-packages/central-package-management).

### Configuration Files

**`Directory.Packages.props`** - Central package version definitions:
```xml
<ItemGroup>
  <PackageVersion Include="Spectre.Console" Version="$(SpectreConsoleVersion)" />
  <PackageVersion Include="System.CommandLine" Version="$(SystemCommandLineVersion)" />
  <PackageVersion Include="MetadataExtractor" Version="$(MetadataExtractorVersion)" />
  <PackageVersion Include="TimeZoneConverter" Version="$(TimeZoneConverterVersion)" />
</ItemGroup>
```

**`eng/Versions.props`** - Version variable definitions:
```xml
<PropertyGroup>
  <SpectreConsoleVersion>0.54.0</SpectreConsoleVersion>
  <SystemCommandLineVersion>2.0.1</SystemCommandLineVersion>
  <MetadataExtractorVersion>2.8.1</MetadataExtractorVersion>
  <TimeZoneConverterVersion>7.2.0</TimeZoneConverterVersion>
</PropertyGroup>
```

### Using Packages in Projects

Projects reference packages **without** specifying versions:
```xml
<ItemGroup>
  <PackageReference Include="Spectre.Console" />
  <PackageReference Include="System.CommandLine" />
</ItemGroup>
```

Versions are automatically resolved from `Directory.Packages.props`.

### Benefits of CPM

✅ **Consistent Versions** - All projects use the same package versions  
✅ **Single Source of Truth** - Update versions in one place  
✅ **Prevent Version Conflicts** - No accidental version mismatches  
✅ **Easier Updates** - Update a package version once for all projects  
✅ **Transitive Pinning** - Control transitive dependency versions

## Benefits

✅ **Single Source of Truth**: All version information in one file  
✅ **Automated Previews**: Automatic preview builds on push to main  
✅ **Consistent Versioning**: Across all projects and packages  
✅ **Central Package Management**: NuGet packages managed with CPM  
✅ **SemVer Compliance**: Follows semantic versioning standards  
✅ **No Version Conflicts**: CPM prevents version mismatches  

## Workflows

### Creating a New Stable Release

1. Update `eng/Versions.props`:
   ```xml
   <MajorVersion>1</MajorVersion>
   <MinorVersion>0</MinorVersion>
   <PatchVersion>0</PatchVersion>
   <PreReleaseVersionLabel></PreReleaseVersionLabel>
   ```

2. Commit and push to `main`

3. Run the release workflow:
   ```bash
   gh workflow run release.yml
   ```

### Creating a Preview Release

1. Update `eng/Versions.props`:
   ```xml
   <MajorVersion>1</MajorVersion>
   <MinorVersion>1</MinorVersion>
   <PatchVersion>0</PatchVersion>
   <PreReleaseVersionLabel>preview.1</PreReleaseVersionLabel>
   ```

2. Commit and push to `main`

3. Run the release workflow:
   ```bash
   gh workflow run release.yml -f prerelease=true
   ```

### Updating Package Dependencies

1. Edit `eng/Versions.props` to update the package version:
   ```xml
   <SpectreConsoleVersion>0.55.0</SpectreConsoleVersion>
   ```

2. The change automatically applies to all projects via `Directory.Packages.props`

3. Commit and push - all projects will use the new version

### Adding New Package Dependencies

1. Add the version variable to `eng/Versions.props`:
   ```xml
   <NewPackageVersion>1.0.0</NewPackageVersion>
   ```

2. Add the package reference to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="NewPackage" Version="$(NewPackageVersion)" />
   ```

3. Reference the package in your project (without version):
   ```xml
   <PackageReference Include="NewPackage" />
   ```

## References

- [Semantic Versioning 2.0.0](https://semver.org/)
- [Central Package Management (CPM)](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [.NET Aspire Versioning](https://github.com/dotnet/aspire/blob/main/eng/Versions.props)
- [MSBuild Properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-properties)
