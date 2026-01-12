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
var listPathOpt = new Option<string>("--path")
{
    Description = "Folder path to scan (defaults to current directory)",
    DefaultValueFactory = _ => "."
};
listCommand.Add(listPathOpt);
listCommand.Add(extensionOption);
listCommand.SetAction(input =>
{
    var path = input.GetValue(listPathOpt)!;
    var ext = input.GetValue(extensionOption);
    return ListCommandHandler.Execute(path, ext);
});

// Preview command - Show rename preview
var previewCommand = new Command("preview", "Preview rename operations");
var previewPathOpt = new Option<string>("--path")
{
    Description = "Folder path (defaults to current directory)",
    DefaultValueFactory = _ => "."
};
var previewPatternOpt = new Option<string?>("--pattern")
{
    Description = "Rename pattern (e.g., {year}-{month}-{day}_{filename})"
};
previewCommand.Add(previewPathOpt);
previewCommand.Add(previewPatternOpt);
previewCommand.Add(extensionOption);
previewCommand.SetAction(input =>
{
    var path = input.GetValue(previewPathOpt)!;
    var pattern = input.GetValue(previewPatternOpt);
    var ext = input.GetValue(extensionOption);
    return PreviewCommandHandler.Execute(path, pattern, ext);
});

// Rename command - Execute rename operations
var renameCommand = new Command("rename", "Execute rename operations");
var renamePathOpt = new Option<string>("--path")
{
    Description = "Folder path (defaults to current directory)",
    DefaultValueFactory = _ => "."
};
var renamePatternOpt = new Option<string?>("--pattern") { Description = "Rename pattern" };
var dryRunOpt = new Option<bool>("--dry-run")
{
    Description = "Preview changes without executing"
};
var noInteractiveOpt = new Option<bool>("--no-interactive")
{
    Description = "Skip confirmation prompts and execute automatically"
};
renameCommand.Add(renamePathOpt);
renameCommand.Add(renamePatternOpt);
renameCommand.Add(dryRunOpt);
renameCommand.Add(noInteractiveOpt);
renameCommand.Add(extensionOption);
renameCommand.SetAction(input =>
{
    var path = input.GetValue(renamePathOpt)!;
    var pattern = input.GetValue(renamePatternOpt);
    var ext = input.GetValue(extensionOption);
    var dryRun = input.GetValue(dryRunOpt);
    var noInteractive = input.GetValue(noInteractiveOpt);
    return RenameCommandHandler.Execute(path, pattern, ext, dryRun, noInteractive);
});

// Patterns command - Show available patterns
var patternsCommand = new Command("patterns", "Show available pattern templates");
patternsCommand.SetAction(_ => PatternsCommandHandler.Execute());

// Pattern command group - Manage custom patterns
var patternCommand = new Command("pattern", "Manage custom rename patterns");

// Pattern add subcommand
var patternAddCommand = new Command("add", "Add a new custom pattern");
var patternAddPatternArg = new Argument<string>("pattern")
{
    Description = "The pattern string (e.g., {year}-{month}-{day}_{filename})"
};
var patternAddDescArg = new Argument<string>("description")
{
    Description = "Description of the pattern"
};
patternAddCommand.Add(patternAddPatternArg);
patternAddCommand.Add(patternAddDescArg);
patternAddCommand.SetAction(input =>
{
    var pattern = input.GetValue(patternAddPatternArg)!;
    var description = input.GetValue(patternAddDescArg)!;
    return PatternManageCommandHandler.Add(pattern, description);
});

// Pattern remove subcommand
var patternRemoveCommand = new Command("remove", "Remove a custom pattern");
var patternRemovePatternArg = new Argument<string>("pattern")
{
    Description = "The pattern string to remove"
};
patternRemoveCommand.Add(patternRemovePatternArg);
patternRemoveCommand.SetAction(input =>
{
    var pattern = input.GetValue(patternRemovePatternArg)!;
    return PatternManageCommandHandler.Remove(pattern);
});

// Pattern list subcommand
var patternListCommand = new Command("list", "List all patterns (default and custom)");
patternListCommand.SetAction(_ => PatternManageCommandHandler.List());

patternCommand.Subcommands.Add(patternAddCommand);
patternCommand.Subcommands.Add(patternRemoveCommand);
patternCommand.Subcommands.Add(patternListCommand);

// Register all commands
rootCommand.Subcommands.Add(webUiCommand);
rootCommand.Subcommands.Add(listCommand);
rootCommand.Subcommands.Add(previewCommand);
rootCommand.Subcommands.Add(renameCommand);
rootCommand.Subcommands.Add(patternsCommand);
rootCommand.Subcommands.Add(patternCommand);

return await rootCommand.Parse(args).InvokeAsync();

