using Helix.Models;

namespace Helix.Agent;

/// <summary>
/// Factory interface for creating CodingAgent instances.
/// </summary>
public interface ICodingAgentFactory
{
    /// <summary>
    /// Creates a new instance of the CodingAgent.
    /// </summary>
    /// <param name="conversation">The conversation context for the agent.</param>
    /// <returns>A new CodingAgent instance.</returns>
    CodingAgent Create(Conversation conversation);
}
