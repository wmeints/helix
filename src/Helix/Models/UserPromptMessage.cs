namespace Helix.Models;

/// <summary>
/// Represents a prompt from the user.
/// </summary>
public class UserPromptMessage : Message
{
    public string Content { get; set; } = string.Empty;

    public UserPromptMessage()
    {
        MessageType = "UserPrompt";
    }
}