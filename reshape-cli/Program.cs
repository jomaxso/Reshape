
using System.CommandLine;
using Reshape.Cli;
using Reshape.Cli.Commands;
using Reshape.Cli.Commands.Patterns;
using Spectre.Console;

// ============================================================================
// CLI Application Entry Point
// ============================================================================

// Interactive mode when no arguments provided
if (args.Length == 0 || !args.Any(arg => arg == "--no-interactive"))
{
    AnsiConsole.Write(
    new FigletText("Reshape CLI")
        .LeftJustified()
        .Color(Color.Cyan1));

    AnsiConsole.MarkupLine("[dim]Batch rename files using metadata patterns[/]\n");

    var commandString = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[cyan]What would you like to do?[/]")
            .PageSize(10)
            .AddChoices(
                "🌐 Run - Start Web UI",
                 "📁 File - Manage Files",
                "🎨 Pattern - Manage Patterns",
                "⬆️ Update - Check for Updates"
            ));

    var command = commandString switch
    {
        "🌐 Run - Start Web UI" => RunCommand.Command,
        "📁 File - Manage Files" => FileCommand.Command,
        "🎨 Pattern - Manage Patterns" => PatternCommand.Command,
        "⬆️ Update - Check for Updates" => UpdateCommand.Command,
        _ => null
    };

    return command is null
        ? 0
        : await command.Parse(args).InvokeAsync();
}

var rootCommand = new RootCommand("Reshape CLI - Batch rename files using metadata patterns")
{
    Options = { GlobalOptions.NoInteractive },
    Subcommands =
        {
            RunCommand.Command,
            FileCommand.Command,
            PatternCommand.Command,
            UpdateCommand.Command,
        }
};

return await rootCommand.Parse(args).InvokeAsync();

