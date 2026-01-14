using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'preview' command to show rename operations before executing them.
/// </summary>
internal sealed class PreviewCommand : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var path = parseResult.GetRequiredValue(GlobalOptions.Path);
        var pattern = parseResult.GetValue(GlobalOptions.Pattern);
        var extensions = parseResult.GetValue(GlobalOptions.Extension);

        try
        {
            if (string.IsNullOrEmpty(pattern))
            {
                AnsiConsole.MarkupLine("[red]Error: --pattern is required[/]");
                return 1;
            }

            var fullPath = Path.GetFullPath(path);
            var preview = FileService.GeneratePreview(fullPath, pattern, extensions);

            DisplayPreviewTable(preview, pattern);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    private static void DisplayPreviewTable(RenamePreviewItem[] preview, string pattern)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Original[/]")
            .AddColumn("[bold]→[/]")
            .AddColumn("[bold]New Name[/]")
            .AddColumn("[bold]Status[/]");

        foreach (var item in preview)
        {
            var status = item.HasConflict ? "[red]⚠ Conflict[/]" :
                        item.OriginalName == item.NewName ? "[dim]No change[/]" : "[green]✓ OK[/]";

            table.AddRow(
                $"[cyan]{Markup.Escape(item.OriginalName)}[/]",
                "→",
                item.HasConflict ? $"[red]{Markup.Escape(item.NewName)}[/]" : $"[green]{Markup.Escape(item.NewName)}[/]",
                status
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[yellow]Pattern:[/] {Markup.Escape(pattern)}");
        AnsiConsole.MarkupLine($"[green]{preview.Count(p => !p.HasConflict && p.OriginalName != p.NewName)} file(s) will be renamed[/]");

        if (preview.Any(p => p.HasConflict))
            AnsiConsole.MarkupLine($"[red]{preview.Count(p => p.HasConflict)} conflict(s) detected[/]");
    }
}
