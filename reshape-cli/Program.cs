using System.CommandLine;
using Reshape.Cli.Commands;
using Reshape.Cli.Commands.Patterns;

return await new RootCommand("Reshape CLI - Batch rename files using metadata patterns")
{
    Action = new ReshapeCliCommand(),
    Options = { GlobalOptions.NoInteractive },
    Subcommands =
    {
        new("run", "Starts the Reshape web UI")
        {
            Action = new RunCommand()
        },
        new("file", "List, rename, and manage files")
        {
            Action = new FileCommand(),
            Subcommands =
            {
                new("list", "List files in a folder")
                {
                    Action = new ListCommand(),
                    Options = { GlobalOptions.Path, GlobalOptions.Extension },
                },
                new("rename", "Execute rename operations")
                {
                    Options = { GlobalOptions.Path, GlobalOptions.Pattern, GlobalOptions.Extension },
                    Action = new RenameCommand()
                },
                new("preview", "Preview rename operations")
                {
                    Action = new PreviewCommand(),
                    Options = { GlobalOptions.Path, GlobalOptions.Pattern, GlobalOptions.Extension },
                }
            },
        },
        new("pattern", "Manage custom rename patterns")
        {
            Action = new PatternCommand(),
            Subcommands =
            {
                PatternCommand.BuildSetCommand(),
                PatternCommand.BuildRemoveCommand(),
                PatternCommand.BuildListCommand()
            },
        },
        new("update", "Update Reshape CLI to the latest version")
        {
            Action = new UpdateCommand(),
            Options = { UpdateCommand.StableOption, UpdateCommand.PrereleaseOption, UpdateCommand.CheckOption }
        }
    },
}.Parse(args).InvokeAsync();

