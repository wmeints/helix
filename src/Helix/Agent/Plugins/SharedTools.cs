using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Helix.Agent.Plugins;

/// <summary>
/// This plugin contains key tools for the agent to do a good job.
/// </summary>
public class SharedTools
{
    /// <summary>
    /// Gets or sets the final tool output status
    /// </summary>
    public bool FinalToolOutputReady { get; set; } = false;

    /// <summary>
    /// Gets or sets the final tool output value
    /// </summary>
    public string FinalToolOutputValue { get; set; } = "";

    /// <summary>
    /// This tool is used by the agent to signal that all work has been completed.
    /// </summary>
    /// <param name="output"></param>
    /// <remarks>
    /// We use this to stop the iteration loop in the agent. If this tool is not called, the agent will continue to
    /// iterate until it reaches the maximum number of iterations defined in the agent code.
    /// </remarks>
    [KernelFunction("final_output")]
    [Description(
        """
        Use this tool to provide the final answer to the user.
        The final output tool MUST be called with final answer to the user.
        """
    )]
    public void FinalToolOutput([Description("The final answer to the user.")] string output)
    {
        FinalToolOutputValue = output;
        FinalToolOutputReady = true;
    }

    public void ResetFinalToolOutput()
    {
        FinalToolOutputReady = false;
        FinalToolOutputValue = "";
    }
}