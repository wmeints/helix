namespace Helix.Models;

/// <summary>
/// Represents a response from the assistant.
/// </summary>
public class AssistantResponse : Message
{
    /// <summary>
    /// Content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
