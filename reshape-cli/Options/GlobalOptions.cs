using System.CommandLine;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Global options that are available across all commands.
/// </summary>
internal static class GlobalOptions
{
    /// <summary>
    /// The global --no-interactive option. Use Recursive=true to inherit to subcommands.
    /// </summary>
    public static readonly Option<bool> NoInteractive = new("--no-interactive")
    {
        Description = "Skip all interactive prompts and require explicit arguments",
        Recursive = true
    };

    public static readonly Option<string> Path = new("--path")
    {
        Description = "Folder path to scan (defaults to current directory)",
        DefaultValueFactory = c =>
        {
            const string defaultPath = ".";

            var noInteractive = c.GetValue(NoInteractive);

            return noInteractive
                ? defaultPath
                : AnsiConsole.Ask(
                "[cyan]Folder path:[/] [dim](press Enter for current directory)[/]",
                    defaultValue: defaultPath);
        }
    };

    public static readonly Option<string[]> Extension = new("--ext")
    {
        Description = "Filter by extensions (e.g., .jpg .png)",
        AllowMultipleArgumentsPerToken = true,
        DefaultValueFactory = c =>
        {
            var noInteractive = c.GetValue(NoInteractive);

            if (noInteractive)
            {
                return [];
            }

            string[] CommonExtensions =
            [
                ".jpg", ".jpeg", ".png", ".heic", ".gif", ".bmp", ".tiff", ".raw",
                ".mp4", ".mov", ".avi", ".txt", ".pdf", ".doc", ".docx"
            ];

            var prompt = new MultiSelectionPrompt<string>()
                       .Title("[yellow]Select file extensions to process:[/]")
                       .PageSize(15)
                       .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                       .AddChoices(CommonExtensions);

            var selected = AnsiConsole.Prompt(prompt);
            return [.. selected];
        }
    };

    public static readonly Option<string?> Pattern = new("--pattern")
    {
        Description = "Rename pattern (e.g., {year}-{month}-{day}_{filename})",
        DefaultValueFactory = pattern =>
        {
            var noInteractive = pattern.GetValue(NoInteractive);

            if (noInteractive)
            {
                return null;
            }

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
    };
}
