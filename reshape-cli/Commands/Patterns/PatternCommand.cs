using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Reshape.Cli.Commands.Patterns;

/// <summary>
/// Handles pattern management commands (add, remove, list).
/// </summary>
internal sealed class PatternCommand : AsynchronousCommandLineAction
{
    public static Command Command => new("pattern", "Manage custom rename patterns")
    {
        Subcommands =
        {
            BuildAddCommand(),
            BuildRemoveCommand(),
            BuildListCommand()
        },
        Action = new PatternCommand()
    };

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);

        if (noInteractive)
        {
            AnsiConsole.MarkupLine("[yellow]Usage:[/] pattern [[list|add|remove]]");
            return 0;
        }

        try
        {
            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices("List patterns", "Add pattern", "Remove pattern(s)")
                    .HighlightStyle(new Style(Color.Cyan1))
            );

            return action switch
            {
                "List patterns" => List(),
                "Add pattern" => InteractiveAdd(),
                "Remove pattern(s)" => InteractiveRemove(),
                _ => 0
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    public static Command BuildAddCommand()
    {
        var command = new Command("add", "Add a new custom pattern");

        var patternArg = new Argument<string?>("pattern")
        {
            Description = "The pattern string (e.g., {year}-{month}-{day}_{filename})",
            Arity = ArgumentArity.ZeroOrOne
        };
        var descArg = new Argument<string?>("description")
        {
            Description = "Description of the pattern",
            Arity = ArgumentArity.ZeroOrOne
        };

        command.Add(patternArg);
        command.Add(descArg);

        command.SetAction(input =>
        {
            var pattern = input.GetValue(patternArg);
            var description = input.GetValue(descArg);
            var noInteractive = input.GetValue(GlobalOptions.NoInteractive);
            return Add(pattern, description, noInteractive);
        });

        return command;
    }

    public static Command BuildRemoveCommand()
    {
        var command = new Command("remove", "Remove a custom pattern");

        var patternArg = new Argument<string?>("pattern")
        {
            Description = "The pattern string to remove",
            Arity = ArgumentArity.ZeroOrOne
        };

        command.Add(patternArg);

        command.SetAction(input =>
        {
            var pattern = input.GetValue(patternArg);
            var noInteractive = input.GetValue(GlobalOptions.NoInteractive);
            return Remove(pattern, noInteractive);
        });

        return command;
    }

    public static Command BuildListCommand()
    {
        var command = new Command("list", "List all patterns (default and custom)");
        command.SetAction(_ => List());
        return command;
    }

    private static int InteractiveAdd(string? defaultPattern = null, string? defaultDescription = null)
    {
        DisplayPlaceholderInfo();

        AnsiConsole.MarkupLine("Add a new custom pattern:");
        AnsiConsole.WriteLine();

        var patternPrompt = new TextPrompt<string>("[cyan]Pattern:[/]")
            .PromptStyle("green")
            .ValidationErrorMessage("[red]Pattern cannot be empty[/]")
            .Validate(p => !string.IsNullOrWhiteSpace(p));

        if (!string.IsNullOrEmpty(defaultPattern))
        {
            patternPrompt.DefaultValue(defaultPattern);
        }

        var pattern = AnsiConsole.Prompt(patternPrompt);

        var descriptionPrompt = new TextPrompt<string>("[cyan]Description:[/]")
            .PromptStyle("green")
            .ValidationErrorMessage("[red]Description cannot be empty[/]")
            .Validate(d => !string.IsNullOrWhiteSpace(d));

        if (!string.IsNullOrEmpty(defaultDescription))
        {
            descriptionPrompt.DefaultValue(defaultDescription);
        }

        var description = AnsiConsole.Prompt(descriptionPrompt);

        AnsiConsole.WriteLine();
        return Add(pattern, description, noInteractive: false);
    }

    private static int InteractiveRemove()
    {
        var customPatterns = ConfigurationService.LoadCustomPatterns();

        if (!customPatterns.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]No custom patterns to remove.[/]");
            return 0;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Select pattern(s) to remove[/]");
        AnsiConsole.WriteLine();

        var choices = customPatterns
            .Select(p => $"{p.Pattern} - {p.Description}")
            .ToList();

        var selectedDescriptions = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[cyan]Patterns:[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more patterns)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                .AddChoices(choices)
        );

        if (!selectedDescriptions.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]No patterns selected.[/]");
            return 0;
        }

        AnsiConsole.WriteLine();
        var successCount = 0;
        var errorCount = 0;

        foreach (var selected in selectedDescriptions)
        {
            // Extract pattern from "pattern - description" format
            var pattern = selected.Split(" - ")[0];

            try
            {
                var removed = ConfigurationService.RemoveCustomPattern(pattern);
                if (removed)
                {
                    AnsiConsole.MarkupLine($"[green]✓ Removed:[/] [cyan]{Markup.Escape(pattern)}[/]");
                    successCount++;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠ Not found:[/] [cyan]{Markup.Escape(pattern)}[/]");
                    errorCount++;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error removing {Markup.Escape(pattern)}: {Markup.Escape(ex.Message)}[/]");
                errorCount++;
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]{successCount} pattern(s) removed[/], [red]{errorCount} error(s)[/]");

        return errorCount > 0 ? 1 : 0;
    }
    public static int Add(string? pattern, string? description, bool noInteractive = false)
    {
        // If no-interactive mode and arguments are missing, show error
        if (noInteractive && (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(description)))
        {
            AnsiConsole.MarkupLine("[red]Error: Both pattern and description are required in non-interactive mode[/]");
            return 1;
        }

        // If arguments are missing, use interactive mode with defaults
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(description))
        {
            return InteractiveAdd(pattern, description);
        }

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

    public static int Remove(string? pattern, bool noInteractive = false)
    {
        // If no-interactive mode and pattern is missing, show error
        if (noInteractive && string.IsNullOrEmpty(pattern))
        {
            AnsiConsole.MarkupLine("[red]Error: Pattern is required in non-interactive mode[/]");
            return 1;
        }

        // If pattern is missing, use interactive mode
        if (string.IsNullOrEmpty(pattern))
        {
            return InteractiveRemove();
        }

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
