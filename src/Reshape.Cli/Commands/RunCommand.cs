using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'run' command to start the Reshape web UI.
/// </summary>
internal sealed class RunCommand : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        var ui = builder.Build();

        ConfigureApiEndpoints(ui);
        ConfigureStaticFiles(ui);
        ConfigureStartupMessage(ui);

        await ui.RunAsync();
        return 0;
    }

    private static void ConfigureApiEndpoints(WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/scan", (ScanRequest request) =>
        {
            try
            {
                var files = FileService.ScanFolder(request.FolderPath, request.Extensions);
                return Results.Ok(new ScanResponse(request.FolderPath, files, files.Length));
            }
            catch (DirectoryNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapGet("/patterns", () => Results.Ok(FileService.GetPatternTemplates()));

        api.MapPost("/patterns/add", (AddPatternRequest request) =>
        {
            try
            {
                ConfigurationService.AddCustomPattern(request.Pattern, request.Description);
                return Results.Ok(new PatternResponse(true, "Pattern added successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new PatternResponse(false, ex.Message));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new PatternResponse(false, $"Failed to add pattern: {ex.Message}"));
            }
        });

        api.MapPost("/patterns/remove", (RemovePatternRequest request) =>
        {
            try
            {
                var removed = ConfigurationService.RemoveCustomPattern(request.Pattern);
                if (removed)
                {
                    return Results.Ok(new PatternResponse(true, "Pattern removed successfully"));
                }
                else
                {
                    return Results.NotFound(new PatternResponse(false, "Pattern not found"));
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new PatternResponse(false, $"Failed to remove pattern: {ex.Message}"));
            }
        });

        api.MapPost("/preview", (RenamePreviewRequest request) =>
        {
            try
            {
                var items = FileService.GeneratePreview(request.FolderPath, request.Pattern, request.Extensions, request.VacationMode);
                var conflictCount = items.Count(i => i.HasConflict);
                return Results.Ok(new RenamePreviewResponse(items, conflictCount));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapPost("/rename", (RenameExecuteRequest request) =>
        {
            try
            {
                var results = FileService.ExecuteRename(request.Items, request.BaseFolderPath, request.DryRun);
                return Results.Ok(new RenameExecuteResponse(
                    results,
                    results.Count(r => r.Success),
                    results.Count(r => !r.Success)
                ));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapGet("/metadata/{*filePath}", (string filePath) =>
        {
            try
            {
                var decodedPath = Uri.UnescapeDataString(filePath);
                if (!System.IO.File.Exists(decodedPath))
                    return Results.NotFound(new { error = "File not found" });

                var metadata = FileService.ExtractMetadata(decodedPath);
                return Results.Ok(metadata);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static void ConfigureStaticFiles(WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    private static void ConfigureStartupMessage(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var addresses = app.Urls;

            var panel = new Panel(
                new Markup($"[bold green]✓ Reshape Web UI is running![/]\n\n" +
                          string.Join("\n", addresses.Select(url =>
                              $"[cyan]→[/] [link={url}]{url}[/]"))))
            {
                Header = new PanelHeader("[yellow]Server Started[/]", Justify.Center),
                Border = BoxBorder.Double,
                BorderStyle = new Style(Color.Green),
                Padding = new Padding(2, 1)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.MarkupLine("\n[dim]Press Ctrl+C to stop the server[/]");
        });
    }
}
