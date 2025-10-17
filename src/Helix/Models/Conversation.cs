using Microsoft.Extensions.AI;
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
    
    /// <summary>
    /// The date the conversation was started.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The list of pending function calls for the conversation.
    /// </summary>
    /// <remarks>
    /// The agent uses this to track function calls that require user permission.
    /// When there are zero pending function calls, the agent can continue processing.
    /// </remarks>
    public List<PendingFunctionCall> PendingFunctionCalls { get; set; } = new();
}