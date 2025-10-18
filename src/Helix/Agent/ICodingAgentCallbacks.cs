namespace Helix.Agent;

/// <summary>
/// Provides a way for the agent to communicate back updates to the caller.
/// </summary>
public interface ICodingAgentCallbacks
{
    /// <summary>
    /// Invoke to send information about an agent response to the client
    /// </summary>
    /// <param name="content">Content of the response</param>
    Task ReceiveAgentResponse(string content);

    /// <summary>
    /// Invoke to send information about a toolcall to the client
    /// </summary>
    /// <param name="toolName">Name of the tool that was used</param>
    /// <param name="arguments">Dictionary of arguments for the tool</param>
    Task ReceiveToolCall(string toolName, Dictionary<string, string> arguments);

    /// <summary>
    /// Invoke to mark the agent task completed.
    /// </summary>
    Task AgentCompleted();

    /// <summary>
    /// Invoke to indicate to the client we reached the maximum number of iterations.
    /// </summary>
    Task MaxIterationsReached();

    /// <summary>
    /// Invoke to indicate to the client that the request was cancelled.
    /// </summary>
    Task RequestCancelled();

    /// <summary>
    /// Request permission from the user to execute the tool.
    /// </summary>
    /// <param name="callId">Identifier for the function call.</param>
    /// <param name="functionName">Function that the agent wants to call.</param>
    /// <param name="arguments">Arguments for the function.</param>
    Task RequestPermission(string callId, string functionName, Dictionary<string, string> arguments);
}