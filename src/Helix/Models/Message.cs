namespace Helix.Models;

/// <summary>
/// Base class for all message types in a conversation.
/// </summary>
public abstract class Message
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The conversation this message belongs to.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// The conversation navigation property.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
