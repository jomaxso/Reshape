
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
        "🌐 Start Web UI" => new ServeCommandHandler().RunInteractiveAsync(),
        "📋 List Files" => ListCommand.Command.Parse(args).InvokeAsync(),
        "👁️  Preview Rename" => new PreviewCommandHandler().RunInteractiveAsync(),
        "✏️  Execute Rename" => new RenameCommandHandler().RunInteractiveAsync(),
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
            PreviewCommandHandler.BuildCommand(),
            RenameCommandHandler.BuildCommand(),
            PatternCommand.Command,
            ServeCommandHandler.BuildCommand(),
        }
};

return await rootCommand.Parse(args).InvokeAsync();

