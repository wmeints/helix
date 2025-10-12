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
    /// <param name="timestamp">Timestamp the response was generated</param>
    Task ReceiveAgentResponse(string content, DateTime timestamp);
    
    /// <summary>
    /// Invoke to send information about a toolcall to the client
    /// </summary>
    /// <param name="toolName">Name of the tool that was used</param>
    /// <param name="arguments">List of arguments for the tool</param>
    /// <param name="timestamp">Timestamp the call happened</param>
    Task ReceiveToolCall(string toolName, List<string> arguments, DateTime timestamp);
    
    /// <summary>
    /// Invoke to mark the agent task completed.
    /// </summary>
    Task AgentCompleted(DateTime timestamp);
    
    /// <summary>
    /// Invoke to indicate to the client we reached the maximum number of iterations.
    /// </summary>
    /// <param name="timestamp">Timestamp for the event.</param>
    Task MaxIterationsReached(DateTime timestamp);
    
    /// <summary>
    /// Invoke to indicate to the client that the request was cancelled.
    /// </summary>
    /// <param name="timestamp">Timestamp of the event.</param>
    Task RequestCancelled(DateTime timestamp);
}