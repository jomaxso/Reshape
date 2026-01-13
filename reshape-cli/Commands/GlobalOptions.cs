using System.CommandLine;

namespace Reshape.Cli.Commands;

/// <summary>
/// Global options that are available across all commands.
/// </summary>
internal static class GlobalOptions
{
    /// <summary>
    /// The global --no-interactive option. Use Recursive=true to inherit to subcommands.
    /// </summary>
    public static readonly Option<bool> NoInteractive = new("--no-interactive")
    {
        Description = "Skip all interactive prompts and require explicit arguments",
        Recursive = true
    };
}
