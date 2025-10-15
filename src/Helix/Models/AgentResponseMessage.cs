namespace Helix.Models;

/// <summary>
/// Represents a response from the agent that is not a tool call.
/// </summary>
public class AgentResponseMessage : Message
{
    public string Content { get; set; } = string.Empty;

    public AgentResponseMessage()
    {
        MessageType = "AgentResponse";
    }
}