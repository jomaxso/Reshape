#:sdk Microsoft.NET.Sdk.Web

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0
#:package MetadataExtractor@2.8.1

using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Spectre.Console;

// ============================================================================
// CLI Commands
// ============================================================================

var webUiCommand = new Command("serve", "Starts the Reshape web user interface");

webUiCommand.SetAction(async _ =>
{
    var builder = WebApplication.CreateSlimBuilder();

    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

    var ui = builder.Build();

    // API Endpoints
    var api = ui.MapGroup("/api");

    api.MapPost("/scan", (ScanRequest request) =>
    {
        try
        {
            var files = FileService.ScanFolder(request.FolderPath, request.Extensions);
            return Results.Ok(new ScanResponse(request.FolderPath, files, files.Length));
        }
        catch (DirectoryNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    api.MapGet("/patterns", () => Results.Ok(FileService.GetPatternTemplates()));

    api.MapPost("/preview", (RenamePreviewRequest request) =>
    {
        try
        {
            var items = FileService.GeneratePreview(request.FolderPath, request.Pattern, request.Extensions);
            var conflictCount = items.Count(i => i.HasConflict);
            return Results.Ok(new RenamePreviewResponse(items, conflictCount));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    api.MapPost("/rename", (RenameExecuteRequest request) =>
    {
        try
        {
            var results = FileService.ExecuteRename(request.Items, request.DryRun);
            return Results.Ok(new RenameExecuteResponse(
                results,
                results.Count(r => r.Success),
                results.Count(r => !r.Success)
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    api.MapGet("/metadata/{*filePath}", (string filePath) =>
    {
        try
        {
            var decodedPath = Uri.UnescapeDataString(filePath);
            if (!System.IO.File.Exists(decodedPath))
                return Results.NotFound(new { error = "File not found" });

            var metadata = FileService.ExtractMetadata(decodedPath);
            return Results.Ok(metadata);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // Static files (Vue app)
    ui.UseDefaultFiles();
    ui.UseStaticFiles();

    ui.Lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = ui.Urls;

        var panel = new Panel(
            new Markup($"[bold green]✓ Reshape Web UI is running![/]\n\n" +
                      string.Join("\n", addresses.Select(url =>
                          $"[cyan]→[/] [link={url}]{url}[/]"))))
        {
            Header = new PanelHeader("[yellow]🚀 Server Started[/]", Justify.Center),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Green),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine("\n[dim]Press Ctrl+C to stop the server[/]");
    });

    await ui.RunAsync();
});

// List command
var listCommand = new Command("list", "List files in a folder");
var pathArgument = new Argument<string>("path") { Description = "Folder path to scan" };
var extensionOption = new Option<string[]>("--ext") { Description = "Filter by extensions (e.g., .jpg .png)", AllowMultipleArgumentsPerToken = true };

listCommand.Arguments.Add(pathArgument);
listCommand.Options.Add(extensionOption);

listCommand.SetAction(input =>
{
    try
    {
        var path = input.GetRequiredValue<string>("path");
        var ext = input.GetValue<string[]>("ext") ?? [];

        var fullPath = Path.GetFullPath(path);
        var files = FileService.ScanFolder(fullPath, ext != null && ext.Length > 0 ? ext : null);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Size[/]", c => c.RightAligned())
            .AddColumn("[bold]Modified[/]")
            .AddColumn("[bold]Metadata[/]");

        foreach (var file in files)
        {
            var metaPreview = string.Join(", ", file.Metadata.Take(3).Select(m => $"{m.Key}={m.Value}"));
            table.AddRow(
                $"[cyan]{Markup.Escape(file.Name)}[/]",
                FormatSize(file.Size),
                file.ModifiedAt.ToString("yyyy-MM-dd HH:mm"),
                $"[dim]{Markup.Escape(metaPreview)}...[/]"
            );
        }

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader($"[yellow]📁 {Markup.Escape(fullPath)}[/]"),
            Border = BoxBorder.Double
        });

        AnsiConsole.MarkupLine($"\n[green]Found {files.Length} file(s)[/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    }
});

// Preview command
var previewCommand = new Command("preview", "Preview rename operations");
var previewPathArg = new Argument<string>("path") { Description = "Folder path" };
var patternOption = new Option<string>("--pattern") { Description = "Rename pattern (e.g., {year}-{month}-{day}_{filename})" };

previewCommand.Arguments.Add(previewPathArg);
previewCommand.Options.Add(patternOption);
previewCommand.Options.Add(extensionOption);

previewCommand.SetAction(input =>
{
    var path = input.GetRequiredValue<string>("path");
    var pattern = input.GetRequiredValue<string>("pattern");
    var ext = input.GetValue<string[]>("ext") ?? [];
    try
    {
        var fullPath = Path.GetFullPath(path);
        var preview = FileService.GeneratePreview(fullPath, pattern, ext != null && ext.Length > 0 ? ext : null);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Original[/]")
            .AddColumn("[bold]→[/]")
            .AddColumn("[bold]New Name[/]")
            .AddColumn("[bold]Status[/]");

        foreach (var item in preview)
        {
            var status = item.HasConflict ? "[red]⚠ Conflict[/]" :
                        item.OriginalName == item.NewName ? "[dim]No change[/]" : "[green]✓ OK[/]";

            table.AddRow(
                $"[cyan]{Markup.Escape(item.OriginalName)}[/]",
                "→",
                item.HasConflict ? $"[red]{Markup.Escape(item.NewName)}[/]" : $"[green]{Markup.Escape(item.NewName)}[/]",
                status
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[yellow]Pattern:[/] {Markup.Escape(pattern)}");
        AnsiConsole.MarkupLine($"[green]{preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName)} file(s) will be renamed[/]");

        if (preview.Any(p => p.HasConflict))
            AnsiConsole.MarkupLine($"[red]{preview.Count(p => p.HasConflict)} conflict(s) detected[/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    }
});

// Rename command
var renameCommand = new Command("rename", "Execute rename operations");
var renamePathArg = new Argument<string>("path") { Description = "Folder path" };
var renamePatternOption = new Option<string>("--pattern") { Description = "Rename pattern" };
var dryRunOption = new Option<bool>("--dry-run") { Description = "Preview changes without executing" };

renameCommand.Add(renamePathArg);
renameCommand.Add(renamePatternOption);
renameCommand.Add(extensionOption);
renameCommand.Add(dryRunOption);

renameCommand.SetAction(input =>
{
    try
    {
        var path = input.GetRequiredValue<string>("path");
        var pattern = input.GetRequiredValue<string>("pattern");
        var ext = input.GetValue<string[]>("ext") ?? [];
        var dryRun = input.GetValue<bool>("dry-run");

        var fullPath = Path.GetFullPath(path);
        var preview = FileService.GeneratePreview(fullPath, pattern, ext != null && ext.Length > 0 ? ext : null);

        if (!dryRun)
        {
            var confirm = AnsiConsole.Confirm($"Rename {preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName)} file(s)?");
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                return;
            }
        }

        var results = FileService.ExecuteRename(preview, dryRun);

        foreach (var result in results)
        {
            var icon = result.Success ? "[green]✓[/]" : "[red]✗[/]";
            var originalName = System.IO.Path.GetFileName(result.OriginalPath);
            var newName = System.IO.Path.GetFileName(result.NewPath);
            AnsiConsole.MarkupLine($"{icon} {Markup.Escape(originalName)} → {Markup.Escape(newName)}");

            if (!result.Success && result.Error != null)
                AnsiConsole.MarkupLine($"  [red]{Markup.Escape(result.Error)}[/]");
        }

        if (dryRun)
            AnsiConsole.MarkupLine("\n[yellow]Dry run - no files were changed[/]");
        else
            AnsiConsole.MarkupLine($"\n[green]Successfully renamed {results.Count(r => r.Success)} file(s)[/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    }
}, renamePathArg, renamePatternOption, extensionOption, dryRunOption);

// Patterns command
var patternsCommand = new Command("patterns", "Show available pattern templates");
patternsCommand.SetAction(input =>
{
    var patterns = FileService.GetPatternTemplates();

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("[bold]Pattern[/]")
        .AddColumn("[bold]Description[/]");

    foreach (var p in patterns)
    {
        table.AddRow($"[cyan]{Markup.Escape(p.Pattern)}[/]", p.Description);
    }

    AnsiConsole.Write(new Panel(table)
    {
        Header = new PanelHeader("[yellow]📝 Available Patterns[/]"),
        Border = BoxBorder.Double
    });

    AnsiConsole.MarkupLine("\n[dim]Available placeholders:[/]");
    AnsiConsole.MarkupLine("[dim]  {filename}, {ext}, {year}, {month}, {day}[/]");
    AnsiConsole.MarkupLine("[dim]  {created}, {modified}, {date_taken}, {time_taken}[/]");
    AnsiConsole.MarkupLine("[dim]  {camera_make}, {camera_model}, {width}, {height}[/]");
    AnsiConsole.MarkupLine("[dim]  {counter:N} - auto-incrementing counter with N digits[/]");
});

var rootCommand = new RootCommand("Reshape CLI - Batch rename files using metadata patterns");

rootCommand.Subcommands.Add(webUiCommand);
rootCommand.Subcommands.Add(patternsCommand);
rootCommand.Subcommands.Add(previewCommand);
rootCommand.Subcommands.Add(renameCommand);
rootCommand.Subcommands.Add(patternsCommand);

return await rootCommand.Parse(args)
    .InvokeAsync();

// Helper function
static string FormatSize(long bytes)
{
    string[] sizes = ["B", "KB", "MB", "GB"];
    var order = 0;
    double size = bytes;
    while (size >= 1024 && order < sizes.Length - 1)
    {
        order++;
        size /= 1024;
    }
    return $"{size:0.##} {sizes[order]}";
}

// ============================================================================
// Models
// ============================================================================

record FileInfo(
    string Name,
    string FullPath,
    string Extension,
    long Size,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    Dictionary<string, string> Metadata
);

record ScanRequest(string FolderPath, string[]? Extensions = null);
record ScanResponse(string FolderPath, FileInfo[] Files, int TotalCount);

record RenamePattern(string Pattern, string Description);
record RenamePreviewRequest(string FolderPath, string Pattern, string[]? Extensions = null);
record RenamePreviewItem(string OriginalName, string NewName, string FullPath, bool HasConflict);
record RenamePreviewResponse(RenamePreviewItem[] Items, int ConflictCount);

record RenameExecuteRequest(RenamePreviewItem[] Items, bool DryRun = false);
record RenameResult(string OriginalPath, string NewPath, bool Success, string? Error);
record RenameExecuteResponse(RenameResult[] Results, int SuccessCount, int ErrorCount);

// ============================================================================
// File Operations Service
// ============================================================================

static class FileService
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".heic"];
    private static readonly string[] VideoExtensions = [".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv"];
    private static readonly string[] AudioExtensions = [".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma"];

    public static FileInfo[] ScanFolder(string folderPath, string[]? extensions = null)
    {
        if (!System.IO.Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

        var files = System.IO.Directory.GetFiles(folderPath)
            .Select(path => new System.IO.FileInfo(path))
            .Where(f => extensions == null || extensions.Length == 0 ||
                        extensions.Any(ext => f.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
            .Select(f => new FileInfo(
                Name: f.Name,
                FullPath: f.FullName,
                Extension: f.Extension.ToLowerInvariant(),
                Size: f.Length,
                CreatedAt: f.CreationTime,
                ModifiedAt: f.LastWriteTime,
                Metadata: ExtractMetadata(f.FullName)
            ))
            .OrderBy(f => f.Name)
            .ToArray();

        return files;
    }

    public static Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var metadata = new Dictionary<string, string>();
        var fileInfo = new System.IO.FileInfo(filePath);
        var ext = fileInfo.Extension.ToLowerInvariant();

        // Basic file metadata
        metadata["filename"] = System.IO.Path.GetFileNameWithoutExtension(filePath);
        metadata["ext"] = ext.TrimStart('.');
        metadata["size"] = fileInfo.Length.ToString();
        metadata["created"] = fileInfo.CreationTime.ToString("yyyy-MM-dd");
        metadata["created_time"] = fileInfo.CreationTime.ToString("HH-mm-ss");
        metadata["modified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd");
        metadata["modified_time"] = fileInfo.LastWriteTime.ToString("HH-mm-ss");
        metadata["year"] = fileInfo.LastWriteTime.Year.ToString();
        metadata["month"] = fileInfo.LastWriteTime.Month.ToString("D2");
        metadata["day"] = fileInfo.LastWriteTime.Day.ToString("D2");

        // Try to extract EXIF data for images
        if (ImageExtensions.Contains(ext))
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);

                // EXIF DateTime
                var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exifSubIfd != null)
                {
                    if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken))
                    {
                        metadata["date_taken"] = dateTaken.ToString("yyyy-MM-dd");
                        metadata["time_taken"] = dateTaken.ToString("HH-mm-ss");
                        metadata["year"] = dateTaken.Year.ToString();
                        metadata["month"] = dateTaken.Month.ToString("D2");
                        metadata["day"] = dateTaken.Day.ToString("D2");
                    }
                }

                // Camera info
                var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (exifIfd0 != null)
                {
                    var make = exifIfd0.GetDescription(ExifDirectoryBase.TagMake);
                    var model = exifIfd0.GetDescription(ExifDirectoryBase.TagModel);
                    if (!string.IsNullOrEmpty(make)) metadata["camera_make"] = SanitizeForFilename(make);
                    if (!string.IsNullOrEmpty(model)) metadata["camera_model"] = SanitizeForFilename(model);
                }

                // Image dimensions
                foreach (var dir in directories)
                {
                    var width = dir.GetDescription(ExifDirectoryBase.TagImageWidth);
                    var height = dir.GetDescription(ExifDirectoryBase.TagImageHeight);
                    if (!string.IsNullOrEmpty(width)) metadata["width"] = width;
                    if (!string.IsNullOrEmpty(height)) metadata["height"] = height;
                }
            }
            catch
            {
                // Silently ignore metadata extraction errors
            }
        }

        return metadata;
    }

    public static string ApplyPattern(string pattern, Dictionary<string, string> metadata)
    {
        var result = pattern;

        foreach (var kvp in metadata)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        // Add counter placeholder support
        result = Regex.Replace(result, @"\{counter(?::(\d+))?\}", m =>
        {
            var padding = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 3;
            return $"{{counter:{padding}}}"; // Keep placeholder for batch processing
        });

        return SanitizeForFilename(result);
    }

    public static string SanitizeForFilename(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return sanitized.Trim();
    }

    public static RenamePreviewItem[] GeneratePreview(string folderPath, string pattern, string[]? extensions = null)
    {
        var files = ScanFolder(folderPath, extensions);
        var results = new List<RenamePreviewItem>();
        var newNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var counter = 1;

        foreach (var file in files)
        {
            var newName = ApplyPattern(pattern, file.Metadata);

            // Handle counter placeholder
            if (newName.Contains("{counter:"))
            {
                var match = Regex.Match(newName, @"\{counter:(\d+)\}");
                if (match.Success)
                {
                    var padding = int.Parse(match.Groups[1].Value);
                    newName = newName.Replace(match.Value, counter.ToString($"D{padding}"));
                    counter++;
                }
            }

            // Add extension
            newName = newName + file.Extension;

            var hasConflict = newNames.Contains(newName) ||
                             (newName != file.Name && System.IO.File.Exists(System.IO.Path.Combine(folderPath, newName)));

            newNames.Add(newName);
            results.Add(new RenamePreviewItem(file.Name, newName, file.FullPath, hasConflict));
        }

        return results.ToArray();
    }

    public static RenameResult[] ExecuteRename(RenamePreviewItem[] items, bool dryRun = false)
    {
        var results = new List<RenameResult>();

        foreach (var item in items.Where(i => !i.HasConflict && i.OriginalName != i.NewName))
        {
            var directory = System.IO.Path.GetDirectoryName(item.FullPath)!;
            var newPath = System.IO.Path.Combine(directory, item.NewName);

            try
            {
                if (!dryRun)
                {
                    System.IO.File.Move(item.FullPath, newPath);
                }
                results.Add(new RenameResult(item.FullPath, newPath, true, null));
            }
            catch (Exception ex)
            {
                results.Add(new RenameResult(item.FullPath, newPath, false, ex.Message));
            }
        }

        return results.ToArray();
    }

    public static RenamePattern[] GetPatternTemplates() =>
    [
        new("{year}-{month}-{day}_{filename}", "Date prefix: 2024-01-15_photo"),
        new("{date_taken}_{time_taken}_{filename}", "EXIF date/time: 2024-01-15_14-30-00_photo"),
        new("{year}/{month}/{filename}", "Year/Month folders (use with caution)"),
        new("{camera_model}_{date_taken}_{counter:4}", "Camera + date + counter: iPhone_2024-01-15_0001"),
        new("{filename}_{counter:3}", "Original name + counter: photo_001"),
        new("IMG_{year}{month}{day}_{counter:4}", "Standard format: IMG_20240115_0001"),
    ];
}
