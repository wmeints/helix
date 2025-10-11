namespace Helix.Models;

/// <summary>
/// Represents a tool call message during a conversation.
/// </summary>
public class ToolCallMessage : Message
{
    public string ToolName { get; set; } = null!;
    public List<string> Arguments { get; set; } = new();
}
