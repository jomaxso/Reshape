using System.CommandLine;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'preview' command to show rename operations before executing them.
/// </summary>
internal sealed class PreviewCommandHandler : ICommandHandler, ICommandBuilder
{
    private static readonly Option<string> PathOpt = new("--path")
    {
        Description = "Folder path (defaults to current directory)",
        DefaultValueFactory = _ => "."
    };
    private static readonly Option<string?> PatternOpt = new("--pattern")
    {
        Description = "Rename pattern (e.g., {year}-{month}-{day}_{filename})"
    };
    private static readonly Option<string[]> ExtensionOption = new("--ext")
    {
        Description = "Filter by extensions (e.g., .jpg .png)",
        AllowMultipleArgumentsPerToken = true
    };

    private string _path = ".";
    private string? _pattern;
    private string[]? _extensions;

    public PreviewCommandHandler() { }

    public PreviewCommandHandler(string path, string? pattern, string[]? extensions = null)
    {
        _path = path;
        _pattern = pattern;
        _extensions = extensions;
    }

    public static Command BuildCommand()
    {
        var command = new Command("preview", "Preview rename operations");

        command.Add(PathOpt);
        command.Add(PatternOpt);
        command.Add(ExtensionOption);

        command.SetAction(async parseResult =>
        {
            var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);
            var handler = new PreviewCommandHandler(
                parseResult.GetValue(PathOpt)!,
                parseResult.GetValue(PatternOpt),
                parseResult.GetValue(ExtensionOption)
            );
            return noInteractive
                ? await handler.RunAsync()
                : await handler.RunInteractiveAsync();
        });

        return command;
    }

    public Task<int> RunInteractiveAsync()
    {
        _path = PromptForPath();
        _extensions = PromptForExtensions();
        _pattern = PromptForPattern();
        return RunAsync();
    }

    public Task<int> RunAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_pattern))
            {
                AnsiConsole.MarkupLine("[red]Error: --pattern is required[/]");
                return Task.FromResult(1);
            }

            var fullPath = Path.GetFullPath(_path);
            var preview = FileService.GeneratePreview(fullPath, _pattern, _extensions);

            DisplayPreviewTable(preview, _pattern);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return Task.FromResult(1);
        }
    }

    private static string PromptForPath()
    {
        return AnsiConsole.Ask(
            "[cyan]Folder path:[/] [dim](press Enter for current directory)[/]",
            defaultValue: ".");
    }

    private static string[]? PromptForExtensions()
    {
        if (!AnsiConsole.Confirm("Filter by file extensions?", defaultValue: false))
            return null;

        var extInput = AnsiConsole.Ask<string>("[cyan]Extensions (space-separated, e.g., .jpg .png):[/]");
        return extInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[yellow]Pattern:[/] {Markup.Escape(pattern)}");
        AnsiConsole.MarkupLine($"[green]{preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName)} file(s) will be renamed[/]");

        if (preview.Any(p => p.HasConflict))
            AnsiConsole.MarkupLine($"[red]{preview.Count(p => p.HasConflict)} conflict(s) detected[/]");
    }
}
