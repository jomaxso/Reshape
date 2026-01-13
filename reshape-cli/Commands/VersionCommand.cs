using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'version' command to display the current Reshape CLI version.
/// </summary>
internal sealed class VersionCommand : AsynchronousCommandLineAction
{
    public static Command Command => new("version", "Display version information")
    {
        Action = new VersionCommand()
    };

    public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        // Remove any build metadata (e.g., +abc123)
        var plusIndex = version.IndexOf('+');
        if (plusIndex >= 0)
        {
            version = version[..plusIndex];
        }

        AnsiConsole.MarkupLine($"[cyan]Reshape CLI[/] version [green]{version}[/]");
        return Task.FromResult(0);
    }
}
