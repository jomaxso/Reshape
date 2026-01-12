#:sdk Microsoft.NET.Sdk.Web

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System.CommandLine;
using Spectre.Console;

var webUiCommand = new Command("serve", "Starts the Reshape web user interface");

webUiCommand.SetAction(async input =>
{
    var builder = WebApplication.CreateSlimBuilder();

    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    var ui = builder.Build();

    ui.UseDefaultFiles();
    ui.UseStaticFiles();

    ui.Lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = ui.Urls;

        var panel = new Panel(
            new Markup($"[bold green]✓ Reshape Web UI is running![/]\n\n" +
                      string.Join("\n", addresses.Select(url =>
                          $"[cyan]→[/] [link={url}]{url}[/]"))))
        {
            Header = new PanelHeader("[yellow]🚀 Server Started[/]", Justify.Center),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Green),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine("\n[dim]Press Ctrl+C to stop the server[/]");
    });

    await ui.RunAsync();
});

var rootCommand = new RootCommand("Reshape CLI");

rootCommand.Subcommands.Add(webUiCommand);

return await rootCommand.Parse(args)
    .InvokeAsync();