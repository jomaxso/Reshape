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
    public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var noInteractive = parseResult.GetValue(GlobalOptions.NoInteractive);

        var path = parseResult.GetPathOrPrompt();

        var availableExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic", ".gif", ".bmp", ".tiff", ".raw",
                                          ".mp4", ".mov", ".avi", ".txt", ".pdf", ".doc", ".docx" };
        
        var extensions = noInteractive
            ? parseResult.GetRequiredValue(GlobalOptions.Extension)
            : AnsiConsole.Prompt(
                CreateMultiSelectionPrompt(availableExtensions))
                .ToArray();

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

    private static MultiSelectionPrompt<string> CreateMultiSelectionPrompt(string[] availableExtensions)
    {
        var prompt = new MultiSelectionPrompt<string>()
            .Title("[yellow]Select file extensions to process:[/]")
            .PageSize(15)
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
            .AddChoices(availableExtensions);

        // Pre-select all extensions
        foreach (var ext in availableExtensions)
        {
            prompt.Select(ext);
        }

        return prompt;
    }


}
