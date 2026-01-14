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
        var path = parseResult.GetRequiredValue(GlobalOptions.Path);
        var extensions = parseResult.GetRequiredValue(GlobalOptions.Extension);

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


}
