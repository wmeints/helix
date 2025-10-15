namespace Helix.Models;

/// <summary>
/// Base class for all message types in a conversation.
/// </summary>
public abstract class Message
{
    public string MessageType { get; protected set; } = string.Empty;
}