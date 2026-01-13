
using System.CommandLine;
using Reshape.Cli;
using Reshape.Cli.Commands;
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

    var command = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[cyan]What would you like to do?[/]")
            .PageSize(10)
            .AddChoices(
                "🌐 Start Web UI",
                "📋 List Files",
                "👁️  Preview Rename",
                "✏️  Execute Rename",
                "🎨 Manage Patterns"
            ));

    var result = command switch
    {
        "🌐 Start Web UI" => ServeCommandHandler.Command.Parse(args).InvokeAsync(),
        "📋 List Files" => ListCommand.Command.Parse(args).InvokeAsync(),
        "👁️  Preview Rename" => PreviewCommand.Command.Parse(args).InvokeAsync(),
        "✏️  Execute Rename" => RenameCommandHandler.Command.Parse(args).InvokeAsync(),
        "🎨 Manage Patterns" => PatternCommand.Command.Parse(args).InvokeAsync(),
        _ => Task.FromResult(0)
    };

    return await result;
}

var rootCommand = new RootCommand("Reshape CLI - Batch rename files using metadata patterns")
{
    Options = { GlobalOptions.NoInteractive },
    Subcommands =
        {
            ListCommand.Command,
            PreviewCommand.Command,
            RenameCommandHandler.Command,
            PatternCommand.Command,
            ServeCommandHandler.Command,
        }
};

return await rootCommand.Parse(args).InvokeAsync();

