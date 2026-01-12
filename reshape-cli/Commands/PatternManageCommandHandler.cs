using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles pattern management commands (add, remove, list).
/// </summary>
internal static class PatternManageCommandHandler
{
    public static int Add(string pattern, string description)
    {
        try
        {
            ConfigurationService.AddCustomPattern(pattern, description);
            AnsiConsole.MarkupLine($"[green]✓ Pattern added successfully![/]");
            AnsiConsole.MarkupLine($"  Pattern: [cyan]{Markup.Escape(pattern)}[/]");
            AnsiConsole.MarkupLine($"  Description: {Markup.Escape(description)}");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to add pattern: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    public static int Remove(string pattern)
    {
        try
        {
            var removed = ConfigurationService.RemoveCustomPattern(pattern);
            
            if (removed)
            {
                AnsiConsole.MarkupLine($"[green]✓ Pattern removed successfully![/]");
                AnsiConsole.MarkupLine($"  Pattern: [cyan]{Markup.Escape(pattern)}[/]");
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Pattern not found: {Markup.Escape(pattern)}[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to remove pattern: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    public static int List()
    {
        try
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
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to list patterns: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
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
