namespace Helix.Models;

/// <summary>
/// Represents a function call that is pending user permission.
/// </summary>
public class PendingFunctionCall
{
    /// <summary>
    /// The identifier for the call in the database.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The name of the function to be called.
    /// </summary>
    public string FunctionName { get; set; }
    
    /// <summary>
    /// The arguments for the function call.
    /// </summary>
    public Dictionary<string, string> Arguments { get; set; } = new();
    
    /// <summary>
    /// The unique identifier for the function call instance for the agent.
    /// </summary>
    public string FunctionCallId { get; set; }
}