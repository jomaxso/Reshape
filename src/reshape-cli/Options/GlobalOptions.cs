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
        Recursive = true,
        DefaultValueFactory = _ => "."
    };

    public static readonly Option<string[]> Extension = new("--ext")
    {
        Description = "Filter by extensions (e.g., .jpg .png)",
        AllowMultipleArgumentsPerToken = true,
        Recursive = true,
        DefaultValueFactory = _ => []
    };

    public static readonly Option<string?> Pattern = new("--pattern")
    {
        Description = "Rename pattern (e.g., {year}-{month}-{day}_{filename})",
        Recursive = true
    };

    public static string GetPathOrPrompt(this ParseResult parseResult) => parseResult.GetValue(NoInteractive)
            ? parseResult.GetRequiredValue(Path)
            : AnsiConsole.Ask(
            "[cyan]Folder path:[/] [dim](press Enter for current directory)[/]",
                defaultValue: ".");
}
