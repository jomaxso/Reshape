
using System.CommandLine;
using System.CommandLine.Invocation;
using Reshape.Cli.Commands;
using Reshape.Cli.Commands.Patterns;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the root command for the Reshape CLI.
/// </summary>
internal sealed class ReshapeCliCommand : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);

        if (noInteractive)
        {
            AnsiConsole.MarkupLine("[red]Error: Please specify a subcommand in non-interactive mode[/]");
            return 1;
        }

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
                    "🎨 Pattern - Manage Patterns"
                ));

        AsynchronousCommandLineAction? command = commandString switch
        {
            "🌐 Run - Start Web UI" => new RunCommand(),
            "📁 File - Manage Files" => new FileCommand(),
            "🎨 Pattern - Manage Patterns" => new PatternCommand(),
            _ => null
        };

        return command is null
            ? 0
            : await command.InvokeAsync(parseResult, cancellationToken);
    }
}