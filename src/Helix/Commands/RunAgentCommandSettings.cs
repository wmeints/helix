using System.ComponentModel;
using Spectre.Console.Cli;

namespace Helix;

/// <summary>
/// Command-line arguments for the application.
/// </summary>
public class RunAgentCommandSettings: CommandSettings
{
    /// <summary>
    /// Gets or sets the target directory for the agent.
    /// </summary>
    [Description("The target directory for the agent.")]
    [CommandOption("--target-directory <TARGET_DIRECTORY>")]   
    public string? TargetDirectory { get; set; }
}