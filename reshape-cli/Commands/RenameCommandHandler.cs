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
            // Interactive mode: prompt for parameters
            if (!noInteractive)
            {
                path = PromptForPath(path);
                pattern = PromptForPattern(pattern);
                ext = PromptForExtensions(ext);
            }
            else if (string.IsNullOrEmpty(pattern))
            {
                // Non-interactive mode requires pattern
                AnsiConsole.MarkupLine("[red]Error: --pattern is required in non-interactive mode[/]");
                return 1;
            }

            ext ??= [];

            var fullPath = Path.GetFullPath(path);
            var preview = FileService.GeneratePreview(fullPath, pattern!, ext != null && ext.Length > 0 ? ext : null);

            // Show preview in interactive mode
            if (!noInteractive)
            {
                DisplayPreviewTable(preview, pattern!);
                AnsiConsole.WriteLine();
            }

            // Confirm operation
            if (!dryRun && !noInteractive && !ConfirmRename(preview))
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

    private static string PromptForPath(string defaultPath)
    {
        var path = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Enter folder path:[/]")
                .DefaultValue(defaultPath)
                .AllowEmpty());
        
        return string.IsNullOrWhiteSpace(path) ? defaultPath : path;
    }

    private static string PromptForPattern(string? defaultPattern)
    {
        var patterns = FileService.GetPatternTemplates();
        var choices = new List<string>();
        
        // Add well-known patterns
        foreach (var p in patterns)
        {
            choices.Add($"{p.Pattern} - {p.Description}");
        }
        
        // Add custom input option
        choices.Add("Custom pattern...");
        
        // Check for custom patterns file
        var customPatternsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".reshape",
            "patterns.json"
        );
        
        // TODO: Load custom patterns from file if exists
        // For now, just use well-known patterns
        
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Select rename pattern:[/]")
                .PageSize(10)
                .AddChoices(choices));
        
        if (selection == "Custom pattern...")
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("[yellow]Enter custom pattern:[/]")
                    .DefaultValue(defaultPattern ?? "{filename}")
                    .AllowEmpty());
        }
        
        // Extract pattern from selection (before the " - " separator)
        var selectedPattern = selection.Split(" - ")[0];
        return selectedPattern;
    }

    private static string[] PromptForExtensions(string[]? defaultExtensions)
    {
        var commonExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic", ".gif", ".bmp", ".tiff", ".raw", ".mp4", ".mov", ".avi", ".txt", ".pdf", ".doc", ".docx" };
        
        var prompt = new MultiSelectionPrompt<string>()
            .Title("[yellow]Select file extensions to process:[/]")
            .PageSize(15)
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
            .AddChoices(commonExtensions);
        
        // Pre-select default extensions if provided
        if (defaultExtensions != null && defaultExtensions.Length > 0)
        {
            foreach (var ext in defaultExtensions)
            {
                if (commonExtensions.Contains(ext))
                {
                    prompt.Select(ext);
                }
            }
        }
        
        var selected = AnsiConsole.Prompt(prompt);
        
        return selected.ToArray();
    }

    private static void DisplayPreviewTable(RenamePreviewItem[] preview, string pattern)
    {
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

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader("[yellow]Preview - Rename Operations[/]"),
            Border = BoxBorder.Rounded
        });
        
        AnsiConsole.MarkupLine($"[yellow]Pattern:[/] {Markup.Escape(pattern)}");
        AnsiConsole.MarkupLine($"[green]{preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName)} file(s) will be renamed[/]");

        if (preview.Any(p => p.HasConflict))
            AnsiConsole.MarkupLine($"[red]{preview.Count(p => p.HasConflict)} conflict(s) detected[/]");
    }

    private static bool ConfirmRename(RenamePreviewItem[] preview)
    {
        var filesToRename = preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName);
        
        if (filesToRename == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No files to rename.[/]");
            return false;
        }
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[yellow]Proceed with renaming {filesToRename} file(s)?[/]")
                .AddChoices(new[] { "Yes, continue", "No, abort" }));
        
        return choice == "Yes, continue";
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
