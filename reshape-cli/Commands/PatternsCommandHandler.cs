using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'patterns' command to display available pattern templates.
/// </summary>
internal static class PatternsCommandHandler
{
    public static int Execute()
    {
        var patterns = FileService.GetPatternTemplates();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Pattern[/]")
            .AddColumn("[bold]Description[/]");

        foreach (var p in patterns)
        {
            table.AddRow($"[cyan]{Markup.Escape(p.Pattern)}[/]", p.Description);
        }

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader("[yellow]📝 Available Patterns[/]"),
            Border = BoxBorder.Double
        });

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
