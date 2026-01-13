using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'update' command to update Reshape CLI to the latest version.
/// </summary>
internal sealed class UpdateCommand : AsynchronousCommandLineAction
{
    private static readonly Option<bool> StableOption = new("--stable")
    {
        Description = "Only update to stable releases (default)",
        Arity = ArgumentArity.Zero
    };

    private static readonly Option<bool> PrereleaseOption = new("--prerelease")
    {
        Description = "Include prerelease versions",
        Arity = ArgumentArity.Zero
    };

    private static readonly Option<bool> CheckOption = new("--check")
    {
        Description = "Check for updates without installing",
        Arity = ArgumentArity.Zero
    };

    public static Command Command => new("update", "Update Reshape CLI to the latest version")
    {
        Options = { StableOption, PrereleaseOption, CheckOption },
        Action = new UpdateCommand()
    };

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var includePrerelease = parseResult.GetValue(PrereleaseOption);
        var checkOnly = parseResult.GetValue(CheckOption);

        try
        {
            AnsiConsole.Write(new Rule("[cyan]Reshape CLI Update[/]").LeftJustified());

            // Get current version
            var currentVersion = GetCurrentVersion();
            AnsiConsole.MarkupLine($"[dim]Current version: [cyan]{currentVersion}[/][/]");

            // Check for updates
            var release = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Checking for updates...", async ctx =>
                {
                    return await CheckForUpdates(includePrerelease, cancellationToken);
                });

            if (release == null)
            {
                AnsiConsole.MarkupLine("[yellow]Could not check for updates. Please check your internet connection.[/]");
                return 1;
            }

            var latestVersion = release.TagName.TrimStart('v');
            
            if (IsNewerVersion(currentVersion, latestVersion))
            {
                AnsiConsole.MarkupLine($"[green]✓ New version available: [bold]{release.TagName}[/][/]");
                
                if (!string.IsNullOrEmpty(release.Name))
                {
                    AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(release.Name)}[/]");
                }

                if (checkOnly)
                {
                    AnsiConsole.MarkupLine("\n[cyan]Run 'reshape update' to install the new version[/]");
                    return 0;
                }

                // Confirm update
                if (!AnsiConsole.Confirm($"\nDo you want to update to {release.TagName}?", defaultValue: true))
                {
                    AnsiConsole.MarkupLine("[yellow]Update cancelled[/]");
                    return 0;
                }

                // Perform update
                await PerformUpdate(release, cancellationToken);
                
                AnsiConsole.MarkupLine($"\n[green]✓ Successfully updated to {release.TagName}![/]");
                AnsiConsole.MarkupLine("[dim]Run 'reshape --version' to verify[/]");
                
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓ You are already running the latest version ({currentVersion})[/]");
                return 0;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error during update: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        // Remove any build metadata (e.g., +abc123)
        var plusIndex = version.IndexOf('+');
        if (plusIndex >= 0)
        {
            version = version[..plusIndex];
        }

        return version;
    }

    private static async Task<GitHubRelease?> CheckForUpdates(bool includePrerelease, CancellationToken cancellationToken)
    {
        const string repo = "jomaxso/Reshape";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Reshape-CLI");

        try
        {
            if (includePrerelease)
            {
                // Get all releases and find the latest (including prereleases)
                var releases = await client.GetFromJsonAsync<GitHubRelease[]>(
                    $"https://api.github.com/repos/{repo}/releases",
                    AppJsonSerializerContext.Default.GitHubReleaseArray,
                    cancellationToken);
                
                return releases?.FirstOrDefault();
            }
            else
            {
                // Get latest stable release
                var release = await client.GetFromJsonAsync(
                    $"https://api.github.com/repos/{repo}/releases/latest",
                    AppJsonSerializerContext.Default.GitHubRelease,
                    cancellationToken);
                
                return release;
            }
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static bool IsNewerVersion(string currentVersion, string latestVersion)
    {
        // Simple version comparison
        if (Version.TryParse(currentVersion, out var current) && 
            Version.TryParse(latestVersion, out var latest))
        {
            return latest > current;
        }

        // Fallback to string comparison
        return string.CompareOrdinal(currentVersion, latestVersion) < 0;
    }

    private static async Task PerformUpdate(GitHubRelease release, CancellationToken cancellationToken)
    {
        // Determine platform-specific asset name
        var assetName = GetAssetNameForPlatform();
        var asset = release.Assets?.FirstOrDefault(a => a.Name == assetName);

        if (asset == null)
        {
            throw new InvalidOperationException($"No compatible binary found for this platform ({assetName})");
        }

        // Download the update
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Downloading update...", async ctx =>
            {
                await DownloadAndInstallUpdate(asset.BrowserDownloadUrl, cancellationToken);
            });
    }

    private static string GetAssetNameForPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "reshape-win-x64.zip";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "reshape-linux-x64.tar.gz";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Prefer ARM64 on macOS (most modern Macs)
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "reshape-osx-arm64.tar.gz"
                : "reshape-osx-x64.tar.gz";
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system");
        }
    }

    private static async Task DownloadAndInstallUpdate(string downloadUrl, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Reshape-CLI");

        // Download to temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), $"reshape-update-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var archivePath = Path.Combine(tempDir, Path.GetFileName(downloadUrl));
            
            // Download
            var response = await client.GetAsync(downloadUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            await using (var fs = File.Create(archivePath))
            {
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            // Extract
            var extractDir = Path.Combine(tempDir, "extract");
            Directory.CreateDirectory(extractDir);

            if (archivePath.EndsWith(".zip"))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, extractDir);
            }
            else if (archivePath.EndsWith(".tar.gz"))
            {
                // For Unix systems, use tar command
                var psi = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-xzf \"{archivePath}\" -C \"{extractDir}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync(cancellationToken);
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException("Failed to extract archive");
                    }
                }
            }

            // Find the new executable
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? "Reshape.Cli.exe" 
                : "Reshape.Cli";
            
            var newExePath = Directory.GetFiles(extractDir, exeName, SearchOption.AllDirectories).FirstOrDefault();
            if (newExePath == null)
            {
                throw new FileNotFoundException($"Could not find {exeName} in the downloaded archive");
            }

            // Get current executable path
            var currentExePath = Environment.ProcessPath 
                ?? Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("Could not determine current executable path");

            // Replace the executable
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, we need to handle locked files
                ReplaceWindowsExecutable(currentExePath, newExePath);
            }
            else
            {
                // On Unix, we can replace directly
                File.Copy(newExePath, currentExePath, overwrite: true);
                
                // Ensure executable permission
                var chmod = Process.Start("chmod", $"+x \"{currentExePath}\"");
                chmod?.WaitForExit();
            }
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private static void ReplaceWindowsExecutable(string currentPath, string newPath)
    {
        // On Windows, try direct replacement first
        try
        {
            File.Copy(newPath, currentPath, overwrite: true);
        }
        catch (IOException)
        {
            // File is locked (we're running from it)
            // Create a batch script to replace after exit
            var batchPath = Path.Combine(Path.GetTempPath(), $"reshape-update-{Guid.NewGuid()}.bat");
            var batchContent = $@"@echo off
timeout /t 2 /nobreak > nul
move /y ""{newPath}"" ""{currentPath}""
del ""%~f0""
";
            File.WriteAllText(batchPath, batchContent);

            // Start the batch script and exit
            var psi = new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);

            AnsiConsole.MarkupLine("\n[yellow]Note: The update will complete after this process exits.[/]");
        }
    }
}

// GitHub API models
internal record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("prerelease")] bool Prerelease,
    [property: JsonPropertyName("assets")] GitHubAsset[]? Assets
);

internal record GitHubAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl
);
