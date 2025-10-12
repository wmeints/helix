using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Models;

/// <summary>
/// Represents a conversation containing multiple messages.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Unique identifier for the conversation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Topic or title of the conversation.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Collection of messages in this conversation.
    /// </summary>
    public ChatHistory ChatHistory { get; set; }
}
