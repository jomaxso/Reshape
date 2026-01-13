using System.CommandLine;

namespace Reshape.Cli.Commands;

/// <summary>
/// Defines the contract for command handlers supporting both interactive and direct execution modes.
/// </summary>
internal interface ICommandHandler
{
    /// <summary>
    /// Executes the command in interactive mode, prompting the user for required parameters.
    /// </summary>
    /// <returns>Exit code (0 for success, non-zero for errors).</returns>
    Task<int> RunInteractiveAsync();

    /// <summary>
    /// Executes the command directly with provided parameters.
    /// </summary>
    /// <returns>Exit code (0 for success, non-zero for errors).</returns>
    Task<int> RunAsync();
}

/// <summary>
/// Defines the contract for building CLI commands with their associated options and arguments.
/// </summary>
internal interface ICommandBuilder
{
    /// <summary>
    /// Builds the command with its options and arguments.
    /// </summary>
    static abstract Command BuildCommand();
}