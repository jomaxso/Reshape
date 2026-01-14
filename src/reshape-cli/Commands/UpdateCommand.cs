using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
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
    public static readonly Option<bool> PreviewOption = new("--preview")
    {
        Description = "Include preview versions",
        Arity = ArgumentArity.Zero
    };

    public static readonly Option<bool> CheckOption = new("--check")
    {
        Description = "Check for updates without installing",
        Arity = ArgumentArity.Zero
    };

    public static readonly Option<string?> VersionOption = new("--version")
    {
        Description = "Install a specific version (e.g., 1.0.0)",
        Arity = ArgumentArity.ZeroOrOne
    };

    public static readonly Option<int?> PrOption = new("--pr")
    {
        Description = "Install a specific pull request build (e.g., 11)",
        Arity = ArgumentArity.ZeroOrOne
    };

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);
        var checkOnly = parseResult.GetValue(CheckOption);
        var includePreview = parseResult.GetValue(PreviewOption);
        var specificVersion = parseResult.GetValue(VersionOption);
        var prNumber = parseResult.GetValue(PrOption);

        // Validate mutually exclusive options
        var optionsCount = new[] { includePreview, specificVersion != null, prNumber != null }.Count(x => x);
        if (optionsCount > 1)
        {
            AnsiConsole.MarkupLine("[red]Error: Cannot specify multiple update options (--preview, --version, --pr).[/]");
            return 1;
        }

        // Handle --pr option
        if (prNumber.HasValue)
        {
            return await InstallPullRequest(prNumber.Value, cancellationToken);
        }

        // Handle --version option
        if (specificVersion != null)
        {
            return await InstallSpecificVersion(specificVersion, cancellationToken);
        }

        // Interactive mode
        if (!noInteractive && !includePreview)
        {
            var choice = await AnsiConsole.PromptAsync(new SelectionPrompt<string>()
                    .Title("[cyan]What would you like to install?[/]")
                    .AddChoices(
                    [
                        "Latest stable",
                        "Latest preview",
                        "Pull request"
                    ]), cancellationToken);

            switch (choice)
            {
                case "Latest stable":
                    includePreview = false;
                    break;
                case "Latest preview":
                    includePreview = true;
                    break;
                case "Pull request":
                    // Fetch and display list of open PRs
                    var openPrs = await FetchOpenPullRequests(cancellationToken);
                    if (openPrs == null || openPrs.Length == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No open pull requests found.[/]");
                        return 1;
                    }

                    var prChoices = openPrs.Select(pr => $"#{pr.Number}: {pr.Title}").ToArray();
                    var selectedPr = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]Select a pull request:[/]")
                            .PageSize(10)
                            .AddChoices(prChoices));

                    var selectedPrNumber = int.Parse(selectedPr.Split(':')[0].TrimStart('#'));
                    return await InstallPullRequest(selectedPrNumber, cancellationToken);
            }
        }
        else if (noInteractive && !includePreview)
        {
            AnsiConsole.MarkupLine("[red]Error: When running in non-interactive mode, you must specify --preview, --version, or --pr.[/]");
            return 1;
        }

        try
        {
            AnsiConsole.Write(new Rule("[cyan]Reshape CLI Update[/]").LeftJustified());

            // Get current version
            var currentVersion = GetCurrentVersion();
            AnsiConsole.MarkupLine($"[dim]Current version: [cyan]{currentVersion}[/][/]");

            // Check for updates
            var releases = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Checking for updates...", async ctx =>
                {
                    return await CheckForUpdates(includePreview, cancellationToken);
                });

            var release = releases.FirstOrDefault();

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

                if (checkOnly == false && !noInteractive)
                {
                    // prompt for confirmation
                    if (!AnsiConsole.Confirm($"\nDo you want to update to {release.TagName}?", defaultValue: true))
                    {
                        AnsiConsole.MarkupLine("[yellow]Update cancelled[/]");
                        return 0;
                    }
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

    private static async Task<int> InstallSpecificVersion(string version, CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.Write(new Rule($"[cyan]Installing Reshape CLI v{version}[/]").LeftJustified());

            const string repo = "jomaxso/Reshape";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Reshape-CLI");

            // Try to fetch the specific release
            var tagName = version.StartsWith('v') ? version : $"v{version}";
            var url = $"https://api.github.com/repos/{repo}/releases/tags/{tagName}";

            var release = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Fetching version {version}...", async ctx =>
                {
                    try
                    {
                        return await client.GetFromJsonAsync(
                            url,
                            AppJsonSerializerContext.Default.GitHubRelease,
                            cancellationToken);
                    }
                    catch (HttpRequestException)
                    {
                        return null;
                    }
                });

            if (release == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Version {version} not found.[/]");
                AnsiConsole.MarkupLine("[dim]Use 'reshape update' without options to see available versions.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]✓ Found version: {release.TagName}[/]");
            if (!string.IsNullOrEmpty(release.Name))
            {
                AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(release.Name)}[/]");
            }

            await PerformUpdate(release, cancellationToken);
            AnsiConsole.MarkupLine($"\n[green]✓ Successfully installed version {release.TagName}![/]");
            AnsiConsole.MarkupLine("[dim]Run 'reshape --version' to verify[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error installing version: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    private static async Task<int> InstallPullRequest(int prNumber, CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.Write(new Rule($"[cyan]Installing from Pull Request #{prNumber}[/]").LeftJustified());

            const string repo = "jomaxso/Reshape";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Reshape-CLI");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            // Get PR information
            var prInfo = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Fetching PR #{prNumber} information...", async ctx =>
                {
                    try
                    {
                        var url = $"https://api.github.com/repos/{repo}/pulls/{prNumber}";
                        return await client.GetFromJsonAsync(
                            url,
                            AppJsonSerializerContext.Default.GitHubPullRequest,
                            cancellationToken);
                    }
                    catch (HttpRequestException)
                    {
                        return null;
                    }
                });

            if (prInfo == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Pull request #{prNumber} not found.[/]");
                return 1;
            }

            if (prInfo.State != "open")
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: PR #{prNumber} is {prInfo.State}[/]");
            }

            AnsiConsole.MarkupLine($"[green]✓ Found PR: {Markup.Escape(prInfo.Title ?? $"#{prNumber}")}[/]");
            AnsiConsole.MarkupLine($"[dim]  Head SHA: {prInfo.Head.Sha[..7]}[/]");

            // Get workflow runs for this PR
            var workflowRuns = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Finding build artifacts...", async ctx =>
                {
                    var url = $"https://api.github.com/repos/{repo}/actions/runs?event=pull_request&head_sha={prInfo.Head.Sha}";
                    try
                    {
                        return await client.GetFromJsonAsync(
                            url,
                            AppJsonSerializerContext.Default.GitHubWorkflowRunsResponse,
                            cancellationToken);
                    }
                    catch (HttpRequestException)
                    {
                        return null;
                    }
                });

            var buildRun = workflowRuns?.WorkflowRuns
                ?.Where(r => r.Name == "Build and Release" && r.Status == "completed" && r.Conclusion == "success")
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (buildRun == null)
            {
                AnsiConsole.MarkupLine("[red]Error: No successful build found for this PR.[/]");
                AnsiConsole.MarkupLine("[dim]Make sure the PR has a completed 'Build and Release' workflow run.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]✓ Found build from {buildRun.CreatedAt:g}[/]");

            // Check if GitHub CLI is available for artifact download
            if (!IsGitHubCliAvailable())
            {
                AnsiConsole.MarkupLine("\n[yellow]GitHub CLI (gh) is required to download PR artifacts.[/]");
                AnsiConsole.MarkupLine("[dim]Install from: https://cli.github.com/[/]");
                AnsiConsole.MarkupLine("[dim]Then authenticate with: gh auth login[/]");
                return 1;
            }

            // Download artifact using gh CLI
            var assetName = GetAssetNameForPlatform().Replace(".zip", "").Replace(".tar.gz", "");
            var tempDir = Path.Combine(Path.GetTempPath(), $"reshape-pr-{prNumber}-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync($"Downloading {assetName}...", async ctx =>
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "gh",
                            Arguments = $"run download {buildRun.Id} --name {assetName} --dir \"{tempDir}\" --repo {repo}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        var process = Process.Start(psi);
                        if (process != null)
                        {
                            await process.WaitForExitAsync(cancellationToken);
                            if (process.ExitCode != 0)
                            {
                                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                                throw new InvalidOperationException($"Failed to download artifact: {error}");
                            }
                        }
                    });

                // Extract the downloaded archive
                var archivePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Path.Combine(tempDir, $"{assetName}.zip")
                    : Path.Combine(tempDir, $"{assetName}.tar.gz");

                if (!File.Exists(archivePath))
                {
                    throw new FileNotFoundException($"Downloaded archive not found: {archivePath}");
                }

                var extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Extracting archive...", async ctx =>
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, extractDir);
                        }
                        else
                        {
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
                    });

                // Install the downloaded binary
                var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "reshape.exe"
                    : "reshape";

                var newExePath = Directory.GetFiles(extractDir, exeName, SearchOption.AllDirectories).FirstOrDefault();
                if (newExePath == null)
                {
                    throw new FileNotFoundException($"Could not find {exeName} in the downloaded artifact");
                }

                var currentExePath = Environment.ProcessPath
                    ?? Process.GetCurrentProcess().MainModule?.FileName
                    ?? throw new InvalidOperationException("Could not determine current executable path");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ReplaceWindowsExecutable(currentExePath, newExePath);
                }
                else
                {
                    File.Copy(newExePath, currentExePath, overwrite: true);
                    var chmod = Process.Start("chmod", $"+x \"{currentExePath}\"");
                    if (chmod != null)
                    {
                        chmod.WaitForExit();
                    }
                }

                AnsiConsole.MarkupLine($"\n[green]✓ Successfully installed build from PR #{prNumber}![/]");
                AnsiConsole.MarkupLine($"[dim]Build: {buildRun.HeadSha[..7]} from {buildRun.CreatedAt:g}[/]");

                return 0;
            }
            finally
            {
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
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error installing PR build: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    private static async Task<GitHubPullRequest[]?> FetchOpenPullRequests(CancellationToken cancellationToken)
    {
        const string repo = "jomaxso/Reshape";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Reshape-CLI");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Fetching open pull requests...", async ctx =>
            {
                try
                {
                    var url = $"https://api.github.com/repos/{repo}/pulls?state=open&sort=updated&direction=desc";
                    var prs = await client.GetFromJsonAsync(
                        url,
                        AppJsonSerializerContext.Default.GitHubPullRequestArray,
                        cancellationToken);
                    return prs;
                }
                catch (HttpRequestException)
                {
                    return null;
                }
            });
    }

    private static bool IsGitHubCliAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            var process = Process.Start(psi);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
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

    private static async Task<GitHubRelease[]> CheckForUpdates(bool includePrerelease, CancellationToken cancellationToken)
    {
        const string repo = "jomaxso/Reshape";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Reshape-CLI");

        try
        {
            if (includePrerelease)
            {
                // Get all releases and find the latest (including prereleases)
                var releases = await client.GetFromJsonAsync(
                    $"https://api.github.com/repos/{repo}/releases",
                    AppJsonSerializerContext.Default.GitHubReleaseArray,
                    cancellationToken);

                return releases?
                    .OrderByDescending(r => r.TagName)
                    .ToArray() ?? [];
            }
            else
            {
                // Get latest stable release
                var release = await client.GetFromJsonAsync(
                    $"https://api.github.com/repos/{repo}/releases/latest",
                    AppJsonSerializerContext.Default.GitHubRelease,
                    cancellationToken);

                return release != null ? [release] : [];
            }
        }
        catch (HttpRequestException)
        {
            return [];
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
                ? "reshape.exe"
                : "reshape";

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

                // Check and add to PATH if needed
                EnsureInPath(currentExePath);
            }
            else
            {
                // On Unix, we can replace directly
                File.Copy(newExePath, currentExePath, overwrite: true);

                // Ensure executable permission
                var chmod = Process.Start("chmod", $"+x \"{currentExePath}\"");
                if (chmod != null)
                {
                    chmod.WaitForExit();
                    if (chmod.ExitCode != 0)
                    {
                        throw new InvalidOperationException("Failed to set executable permission on updated binary");
                    }
                }
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

    private static void EnsureInPath(string executablePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrEmpty(directory))
                return;

            // Get current user PATH
            var userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;

            // Check if directory is already in PATH (case-insensitive)
            var pathDirs = userPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var isInPath = pathDirs.Any(p => string.Equals(
                Path.GetFullPath(p.Trim()),
                Path.GetFullPath(directory),
                StringComparison.OrdinalIgnoreCase));

            if (!isInPath)
            {
                AnsiConsole.MarkupLine("\n[cyan]Adding installation directory to user PATH...[/]");

                var newPath = string.IsNullOrEmpty(userPath)
                    ? directory
                    : $"{userPath.TrimEnd(';')};{directory}";

                Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.User);

                AnsiConsole.MarkupLine($"[green]✓ Added to PATH: {Markup.Escape(directory)}[/]");
                AnsiConsole.MarkupLine("[yellow]⚠ Please restart your terminal for PATH changes to take effect[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not add to PATH: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[dim]You may need to add the installation directory to your PATH manually[/]");
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

internal record GitHubPullRequest(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("head")] GitHubPullRequestHead Head
);

internal record GitHubPullRequestHead(
    [property: JsonPropertyName("sha")] string Sha
);

internal record GitHubWorkflowRunsResponse(
    [property: JsonPropertyName("workflow_runs")] GitHubWorkflowRun[]? WorkflowRuns
);

internal record GitHubWorkflowRun(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("conclusion")] string? Conclusion,
    [property: JsonPropertyName("head_sha")] string HeadSha,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt
);
