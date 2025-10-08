using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SpecForge.Filters;
using SpecForge.Shared;

namespace Helix.Agents;

public class CodingAgent
{
    private ChatHistoryAgent _agent;
    private ChatHistory _chatHistory;
    private ChatHistoryAgentThread _agentThread;
    private bool _running;
    private FunctionCallReportingFilter _functionCallReportingFilter;

    public CodingAgent(Kernel kernel)
    {
        var agentKernel = kernel.Clone();
        _chatHistory = new ChatHistory();
        _agentThread = new ChatHistoryAgentThread(_chatHistory);
        _functionCallReportingFilter = new FunctionCallReportingFilter();

        agentKernel.FunctionInvocationFilters.Add(_functionCallReportingFilter);
        
        var promptTemplateConfig = new PromptTemplateConfig
        {
            Template = EmbeddedResource.Read("Prompts.Instructions.txt"),
            TemplateFormat = "handlebars",
        };

        _agent = new ChatCompletionAgent(promptTemplateConfig, new HandlebarsPromptTemplateFactory())
        {
            Kernel = kernel,
            Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            })
        };
    }

    public bool Running => _running;

    public async Task<string?> InvokeAsync(string prompt, CodingAgentCallContext callContext,
        CancellationToken cancellationToken = default)
    {
        _functionCallReportingFilter.CallContext = callContext;

        _chatHistory.AddUserMessage(prompt);

        var outputBuilder = new StringBuilder();

        try
        {
            _running = true;

            var responseStream = _agent
                .InvokeAsync(_agentThread).WithCancellation(cancellationToken);

            callContext.UpdateStatus("Generating response...");
            
            await foreach (var chunk in responseStream)
            {
                outputBuilder.Append(chunk.Message.Content);
            }
        }
        finally
        {
            _running = false;
        }

        return outputBuilder.ToString();
    }
}
