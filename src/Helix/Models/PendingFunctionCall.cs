
using Microsoft.SemanticKernel;

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
    public string FunctionCallId { get; set; } = null!;
    
    /// <summary>
    /// Creates a new pending function call from the given function call content.
    /// </summary>
    /// <param name="content">Function call to save.</param>
    /// <returns>Returns the pending function call.</returns>
    public static PendingFunctionCall FromFunctionCallContent(FunctionCallContent content)
    {
        var pendingCall = new PendingFunctionCall
        {
            Id = Guid.NewGuid(),
            FunctionName = content.FunctionName,
            FunctionCallId = content.Id!
        };

        if (content.Arguments is not null)
        {
            foreach (var (key, value) in content.Arguments)
            {
                pendingCall.Arguments[key] = value?.ToString() ?? "";
            }
        }

        return pendingCall;
    }
}