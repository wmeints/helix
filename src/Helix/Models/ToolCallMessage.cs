namespace Helix.Models;

/// <summary>
/// Represents a tool call made by the agent, including the tool name, arguments, and response.
/// </summary>
public class ToolCallMessage : Message
{
    public string ToolName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;

    public ToolCallMessage()
    {
        MessageType = "ToolCall";
    }
}