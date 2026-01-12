using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'rename' command to execute rename operations on files.
/// </summary>
internal static class RenameCommandHandler
{
    public static int Execute(string path, string? pattern, string[]? ext, bool dryRun, bool noInteractive = false)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern))
            {
                AnsiConsole.MarkupLine("[red]Error: --pattern is required[/]");
                return 1;
            }

            ext ??= [];

            var fullPath = Path.GetFullPath(path);
            var preview = FileService.GeneratePreview(fullPath, pattern, ext != null && ext.Length > 0 ? ext : null);

            if (!dryRun && !noInteractive && !ConfirmRename(preview, fullPath, pattern, ext))
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                return 0;
            }

            var results = FileService.ExecuteRename(preview, fullPath, dryRun);

            DisplayResults(results, dryRun);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    private static bool ConfirmRename(RenamePreviewItem[] preview, string path, string pattern, string[]? extensions)
    {
        var filesToRename = preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName);
        
        // Display operation details
        AnsiConsole.MarkupLine($"[yellow]Path:[/] {Markup.Escape(path)}");
        AnsiConsole.MarkupLine($"[yellow]Pattern:[/] {Markup.Escape(pattern)}");
        if (extensions != null && extensions.Length > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Extensions:[/] {string.Join(", ", extensions)}");
        }
        AnsiConsole.MarkupLine($"[yellow]Files to rename:[/] {filesToRename}");
        AnsiConsole.WriteLine();
        
        return AnsiConsole.Confirm($"Proceed with rename?");
    }

    private static void DisplayResults(RenameResult[] results, bool dryRun)
    {
        foreach (var result in results)
        {
            var icon = result.Success ? "[green]✓[/]" : "[red]✗[/]";
            var originalName = Path.GetFileName(result.OriginalPath);
            var newName = Path.GetFileName(result.NewPath);
            AnsiConsole.MarkupLine($"{icon} {Markup.Escape(originalName)} → {Markup.Escape(newName)}");

            if (!result.Success && result.Error != null)
                AnsiConsole.MarkupLine($"  [red]{Markup.Escape(result.Error)}[/]");
        }

        if (dryRun)
            AnsiConsole.MarkupLine("\n[yellow]Dry run - no files were changed[/]");
        else
            AnsiConsole.MarkupLine($"\n[green]Successfully renamed {results.Count(r => r.Success)} file(s)[/]");
    }
}
