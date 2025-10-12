namespace Helix.Agent;

/// <summary>
/// Defines the context information for the agent.
/// </summary>
public class CodingAgentContext
{
    /// <summary>
    /// Gets the working directory for the agent.
    /// </summary>
    public string WorkingDirectory { get; init; }
    
    /// <summary>
    /// Gets the operating the system the agent is running on.
    /// </summary>
    public string OperatingSystem { get; init; }

    /// <summary>
    /// Gets the current date/time
    /// </summary>
    public DateTime CurrentDateTime { get; init; } = DateTime.Now;
}