using Spectre.Console;
using Reshape.Cli.Utilities;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'list' command to display files in a folder.
/// </summary>
internal static class ListCommandHandler
{
    public static int Execute(string path, string[]? ext)
    {
        try
        {
            ext ??= [];

            var fullPath = Path.GetFullPath(path);
            var files = FileService.ScanFolder(fullPath, ext != null && ext.Length > 0 ? ext : null);

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Size[/]", c => c.RightAligned())
                .AddColumn("[bold]Modified[/]")
                .AddColumn("[bold]Metadata[/]");

            foreach (var file in files)
            {
                var metaPreview = string.Join(", ", file.Metadata.Take(3).Select(m => $"{m.Key}={m.Value}"));
                table.AddRow(
                    $"[cyan]{Markup.Escape(file.Name)}[/]",
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

            AnsiConsole.MarkupLine($"\n[green]Found {files.Length} file(s)[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }
}
