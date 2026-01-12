using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'rename' command to execute rename operations on files.
/// </summary>
internal static class RenameCommandHandler
{
    public static int Execute(string path, string? pattern, string[]? ext, bool dryRun)
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

            if (!dryRun && !ConfirmRename(preview))
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

    private static bool ConfirmRename(RenamePreviewItem[] preview)
    {
        var filesToRename = preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName);
        return AnsiConsole.Confirm($"Rename {filesToRename} file(s)?");
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
