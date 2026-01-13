using System.CommandLine;
using Spectre.Console;
using Reshape.Cli.Utilities;
using System.CommandLine.Invocation;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'list' command to display files in a folder.
/// </summary>
internal sealed class ListCommand : AsynchronousCommandLineAction
{
    private static readonly Option<string> PathOption = new("--path")
    {
        Description = "Folder path to scan (defaults to current directory)",
        DefaultValueFactory = c =>
        {
            var noInteractive = c.GetValue(GlobalOptions.NoInteractive);
            return noInteractive ? "." : PromptForPath();
        }
    };
    private static readonly Option<string[]> ExtensionOption = new("--ext")
    {
        Description = "Filter by extensions (e.g., .jpg .png)",
        AllowMultipleArgumentsPerToken = true,
        DefaultValueFactory = c =>
        {
            var noInteractive = c.GetValue(GlobalOptions.NoInteractive);
            return noInteractive ? [] : PromptForExtensions() ?? [];
        }
    };

    public static Command Command => new("list", "List files in a folder")
    {
        Options = { PathOption, ExtensionOption },
        Action = new ListCommand()
    };

    public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);

        var path = parseResult.GetRequiredValue(PathOption);
        var extensions = parseResult.GetRequiredValue(ExtensionOption);

        try
        {
            var fullPath = Path.GetFullPath(path);
            var files = FileService.ScanFolder(fullPath, extensions);

            DisplayFilesTable(files, fullPath);

            AnsiConsole.MarkupLine($"\n[green]Found {files.Length} file(s)[/]");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return Task.FromResult(1);
        }
    }

    private static string PromptForPath()
    {
        return AnsiConsole.Ask(
            "[cyan]Folder path:[/] [dim](press Enter for current directory)[/]",
            defaultValue: ".");
    }

    private static string[]? PromptForExtensions()
    {
        if (!AnsiConsole.Confirm("Filter by file extensions?", defaultValue: false))
            return null;

        var extInput = AnsiConsole.Ask<string>("[cyan]Extensions (space-separated, e.g., .jpg .png):[/]");
        return extInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static void DisplayFilesTable(FileInfo[] files, string fullPath)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Folder[/]")
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Selected[/]")
            .AddColumn("[bold]Size[/]", c => c.RightAligned())
            .AddColumn("[bold]Modified[/]")
            .AddColumn("[bold]Metadata[/]");

        foreach (var file in files)
        {
            var metaPreview = string.Join(", ", file.Metadata.Take(3).Select(m => $"{m.Key}={m.Value}"));
            var folderDisplay = string.IsNullOrEmpty(file.RelativePath) ? "." : file.RelativePath;
            var selectedIcon = file.IsSelected ? "[green]✓[/]" : "[dim]○[/]";

            table.AddRow(
                $"[dim]{Markup.Escape(folderDisplay)}[/]",
                $"[cyan]{Markup.Escape(file.Name)}[/]",
                selectedIcon,
                FormatHelper.FormatSize(file.Size),
                file.ModifiedAt.ToString("yyyy-MM-dd HH:mm"),
                $"[dim]{Markup.Escape(metaPreview)}...[/]"
            );
        }

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader($"[yellow]📁 {Markup.Escape(fullPath)}[/]"),
            Border = BoxBorder.Double
        });
    }


}
