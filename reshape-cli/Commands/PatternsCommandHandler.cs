using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'patterns' command to display available pattern templates.
/// </summary>
internal static class PatternsCommandHandler
{
    public static int Execute()
    {
        var defaultPatterns = ConfigurationService.GetDefaultPatterns();
        var customPatterns = ConfigurationService.LoadCustomPatterns();

        // Display default patterns
        var defaultTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Pattern[/]")
            .AddColumn("[bold]Description[/]");

        foreach (var p in defaultPatterns)
        {
            defaultTable.AddRow($"[cyan]{Markup.Escape(p.Pattern)}[/]", Markup.Escape(p.Description));
        }

        AnsiConsole.Write(new Panel(defaultTable)
        {
            Header = new PanelHeader("[yellow]📝 Default Patterns[/]"),
            Border = BoxBorder.Double
        });

        // Display custom patterns if any exist
        if (customPatterns.Any())
        {
            AnsiConsole.WriteLine();
            
            var customTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Pattern[/]")
                .AddColumn("[bold]Description[/]");

            foreach (var p in customPatterns)
            {
                customTable.AddRow($"[green]{Markup.Escape(p.Pattern)}[/]", Markup.Escape(p.Description));
            }

            AnsiConsole.Write(new Panel(customTable)
            {
                Header = new PanelHeader("[yellow]⭐ Custom Patterns[/]"),
                Border = BoxBorder.Double
            });
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]No custom patterns defined. Use 'pattern add' to create one.[/]");
        }

        DisplayPlaceholderInfo();

        return 0;
    }

    private static void DisplayPlaceholderInfo()
    {
        AnsiConsole.MarkupLine("\n[dim]Available placeholders:[/]");
        AnsiConsole.MarkupLine("[dim]  {filename}, {ext}, {year}, {month}, {day}[/]");
        AnsiConsole.MarkupLine("[dim]  {created}, {modified}, {date_taken}, {time_taken}[/]");
        AnsiConsole.MarkupLine("[dim]  {camera_make}, {camera_model}, {width}, {height}[/]");
        AnsiConsole.MarkupLine("[dim]  {counter:N} - auto-incrementing counter with N digits[/]");
    }
}
