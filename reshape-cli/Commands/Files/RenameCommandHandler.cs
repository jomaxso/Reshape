using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'rename' command to execute rename operations on files.
/// </summary>
internal sealed class RenameCommandHandler : AsynchronousCommandLineAction
{
    private static readonly string[] CommonExtensions =
    [
        ".jpg", ".jpeg", ".png", ".heic", ".gif", ".bmp", ".tiff", ".raw",
        ".mp4", ".mov", ".avi", ".txt", ".pdf", ".doc", ".docx"
    ];

    private static readonly Option<bool> DryRunOption = new("--dry-run")
    {
        Description = "Preview changes without executing"
    };
    private static readonly Option<string[]> ExtensionOption = new("--ext")
    {
        Description = "Filter by extensions (e.g., .jpg .png)",
        AllowMultipleArgumentsPerToken = true
    };

    public static Command Command => new("rename", "Execute rename operations")
    {
        Options = { GlobalOptions.Path, GlobalOptions.Pattern, DryRunOption, GlobalOptions.Extension },
        Action = new RenameCommandHandler()
    };

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);
        var path = parseResult.GetRequiredValue(GlobalOptions.Path);
        var pattern = parseResult.GetValue(GlobalOptions.Pattern);
        var extensions = parseResult.GetValue(GlobalOptions.Extension);
        var dryRun = parseResult.GetValue(DryRunOption);

        // Interactive mode if pattern is missing and not in no-interactive mode
        if (!noInteractive && string.IsNullOrEmpty(pattern))
        {
            path = PromptForPath();
            extensions = PromptForExtensions();
            pattern = PromptForPattern();
            dryRun = AnsiConsole.Confirm("Dry run (preview only)?", defaultValue: true);
        }

        try
        {
            if (string.IsNullOrEmpty(pattern))
            {
                AnsiConsole.MarkupLine("[red]Error: --pattern is required in non-interactive mode[/]");
                return 1;
            }

            var fullPath = Path.GetFullPath(path);
            var preview = FileService.GeneratePreview(fullPath, pattern, extensions);

            // Show preview in interactive mode
            if (!noInteractive)
            {
                DisplayPreviewTable(preview, pattern);
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

    private static string PromptForPath()
    {
        return AnsiConsole.Ask(
            "[cyan]Folder path:[/] [dim](press Enter for current directory)[/]",
            defaultValue: ".");
    }

    private static string PromptForPattern()
    {
        var patterns = FileService.GetPatternTemplates();
        var patternChoices = patterns.Select(p => $"{p.Pattern} - {p.Description}").ToList();
        patternChoices.Add("Custom pattern");

        var selectedPattern = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Select a pattern:[/]")
                .PageSize(15)
                .AddChoices(patternChoices));

        if (selectedPattern == "Custom pattern")
        {
            return AnsiConsole.Ask<string>("[cyan]Enter custom pattern:[/]");
        }

        return selectedPattern.Split(" - ")[0];
    }

    private static string[]? PromptForExtensions()
    {
        var prompt = new MultiSelectionPrompt<string>()
            .Title("[yellow]Select file extensions to process:[/]")
            .PageSize(15)
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
            .AddChoices(CommonExtensions);

        var selected = AnsiConsole.Prompt(prompt);
        return selected.Count > 0 ? selected.ToArray() : null;
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
                .AddChoices("Yes, continue", "No, abort"));

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
