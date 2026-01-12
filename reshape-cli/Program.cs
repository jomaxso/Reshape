using System.CommandLine;
using Reshape.Cli.Commands;

// ============================================================================
// CLI Application Entry Point
// ============================================================================

var rootCommand = new RootCommand("Reshape CLI - Batch rename files using metadata patterns");

// Configure common options
var extensionOption = new Option<string[]>("--ext")
{
    Description = "Filter by extensions (e.g., .jpg .png)",
    AllowMultipleArgumentsPerToken = true
};

// Serve command - Web UI
var webUiCommand = new Command("serve", "Starts the Reshape web user interface");
webUiCommand.SetAction(async _ => await ServeCommandHandler.ExecuteAsync());

// List command - Display files
var listCommand = new Command("list", "List files in a folder");
var listPathArg = new Argument<string>("path") { Description = "Folder path to scan" };
listCommand.Add(listPathArg);
listCommand.Add(extensionOption);
listCommand.SetAction(input =>
{
    var path = input.GetRequiredValue(listPathArg);
    var ext = input.GetValue(extensionOption);
    return ListCommandHandler.Execute(path, ext);
});

// Preview command - Show rename preview
var previewCommand = new Command("preview", "Preview rename operations");
var previewPathArg = new Argument<string>("path") { Description = "Folder path" };
var previewPatternOpt = new Option<string?>("--pattern")
{
    Description = "Rename pattern (e.g., {year}-{month}-{day}_{filename})"
};
previewCommand.Add(previewPathArg);
previewCommand.Add(previewPatternOpt);
previewCommand.Add(extensionOption);
previewCommand.SetAction(input =>
{
    var path = input.GetRequiredValue(previewPathArg);
    var pattern = input.GetValue(previewPatternOpt);
    var ext = input.GetValue(extensionOption);
    return PreviewCommandHandler.Execute(path, pattern, ext);
});

// Rename command - Execute rename operations
var renameCommand = new Command("rename", "Execute rename operations");
var renamePathArg = new Argument<string>("path") { Description = "Folder path" };
var renamePatternOpt = new Option<string?>("--pattern") { Description = "Rename pattern" };
var dryRunOpt = new Option<bool>("--dry-run")
{
    Description = "Preview changes without executing"
};
renameCommand.Add(renamePathArg);
renameCommand.Add(renamePatternOpt);
renameCommand.Add(dryRunOpt);
renameCommand.Add(extensionOption);
renameCommand.SetAction(input =>
{
    var path = input.GetRequiredValue(renamePathArg);
    var pattern = input.GetValue(renamePatternOpt);
    var ext = input.GetValue(extensionOption);
    var dryRun = input.GetValue(dryRunOpt);
    return RenameCommandHandler.Execute(path, pattern, ext, dryRun);
});

// Patterns command - Show available patterns
var patternsCommand = new Command("patterns", "Show available pattern templates");
patternsCommand.SetAction(_ => PatternsCommandHandler.Execute());

// Register all commands
rootCommand.Subcommands.Add(webUiCommand);
rootCommand.Subcommands.Add(listCommand);
rootCommand.Subcommands.Add(previewCommand);
rootCommand.Subcommands.Add(renameCommand);
rootCommand.Subcommands.Add(patternsCommand);

return await rootCommand.Parse(args).InvokeAsync();

