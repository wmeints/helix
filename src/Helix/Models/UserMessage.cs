namespace Helix.Models;

/// <summary>
/// Represents a message sent by the user.
/// </summary>
public class UserMessage : Message
{
    public string Content { get; set; } = null!;
}
