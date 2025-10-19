using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Agent;

/// <summary>
/// Interface for managing agent instructions including system and custom instructions.
/// </summary>
public interface IAgentInstructions
{
    /// <summary>
    /// Injects system and custom instructions into the chat history.
    /// </summary>
    /// <param name="chatHistory">The chat history to inject instructions into.</param>
    /// <param name="context">The agent context containing environment information.</param>
    Task InjectAsync(ChatHistory chatHistory, CodingAgentContext context);

    /// <summary>
    /// Removes system and custom instructions from the chat history.
    /// </summary>
    /// <param name="chatHistory">The chat history to remove instructions from.</param>
    void Remove(ChatHistory chatHistory);
}
