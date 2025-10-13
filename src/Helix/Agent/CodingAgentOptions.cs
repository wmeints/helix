namespace Helix.Agent;

/// <summary>
/// Application-level options for the coding agent.
/// </summary>
public class CodingAgentOptions
{
    /// <summary>
    /// Gets or sets the target directory for the agent.
    /// </summary>
    public string TargetDirectory { get; set; } = null!;
}