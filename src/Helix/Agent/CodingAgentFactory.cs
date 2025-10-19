using Helix.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Helix.Agent;

/// <summary>
/// Factory for creating CodingAgent instances with appropriate context.
/// </summary>
public class CodingAgentFactory : ICodingAgentFactory
{
    private readonly Kernel _applicationKernel;
    private readonly IOptions<CodingAgentOptions> _codingAgentOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IAgentInstructions _agentInstructions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodingAgentFactory"/> class.
    /// </summary>
    /// <param name="applicationKernel">The Semantic Kernel instance to use for the agent.</param>
    /// <param name="codingAgentOptions">The configuration options for the coding agent.</param>
    /// <param name="agentInstructions">The agent instructions reader.</param>
    /// <param name="loggerFactory">The logger factory for creating logger instances.</param>
    public CodingAgentFactory(Kernel applicationKernel, IOptions<CodingAgentOptions> codingAgentOptions, IAgentInstructions agentInstructions, ILoggerFactory loggerFactory)
    {
        _applicationKernel = applicationKernel;
        _codingAgentOptions = codingAgentOptions;
        _agentInstructions = agentInstructions;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a new instance of the CodingAgent.
    /// </summary>
    /// <param name="conversation">The conversation context for the agent.</param>
    /// <returns>A new CodingAgent instance.</returns>
    public CodingAgent Create(Conversation conversation)
    {
        var codingAgentContext = new CodingAgentContext
        {
            TargetDirectory = _codingAgentOptions.Value.TargetDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.UtcNow
        };

        var logger = _loggerFactory.CreateLogger<CodingAgent>();

        return new CodingAgent(_applicationKernel, conversation, codingAgentContext, _agentInstructions, logger);
    }
}
