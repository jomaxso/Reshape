using System.CommandLine;
using Spectre.Console;
using Reshape.Cli.Utilities;
using System.CommandLine.Invocation;
using System.ComponentModel.Design;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'file' command to list, rename, and manage files.
/// </summary>
internal sealed class FileCommand : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);

        if (noInteractive)
        {
            AnsiConsole.MarkupLine("[red]Error: Please specify a subcommand in non-interactive mode[/]");
            return 1;
        }

        var commandString = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[cyan]What would you like to do?[/]")
            .PageSize(10)
            .AddChoices(
                "📄 List Files",
                "✏️ Rename Files",
                "🔍 Preview Rename"
            ));

        AsynchronousCommandLineAction? command = commandString switch
        {
            "📄 List Files" => new ListCommand(),
            "✏️ Rename Files" => new RenameCommand(),
            "🔍 Preview Rename" => new PreviewCommand(),
            _ => null
        };

        return command is null
            ? 0
            : await command.InvokeAsync(parseResult, cancellationToken);
    }
}
