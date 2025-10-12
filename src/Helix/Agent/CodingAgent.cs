using System.Runtime.CompilerServices;
using System.Text;
using Helix.Agent.Plugins;
using Helix.Agent.Plugins.Shell;
using Helix.Agent.Plugins.TextEditor;
using Helix.Hubs;
using Helix.Models;
using Helix.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Helix.Agent;

/// <summary>
/// The coding agent is the core of Helix. It connects the LLM to tools to complete coding tasks.
/// </summary>
/// <remarks>
/// We expect the agent to be transient, created for each coding task request.
/// You can include chat history from previous requests to provide context.
/// </remarks>
public class CodingAgent
{
    private const int MaxIterations = 20;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ChatHistoryAgent _agent;
    private readonly ChatHistory _chatHistory;
    private readonly ChatHistoryAgentThread _agentThread;
    private readonly SharedTools _sharedTools;
    private bool _running;
    
    public CodingAgent(Kernel kernel, ChatHistory chatHistory, CodingAgentContext context)
    {
        var agentKernel = kernel.Clone();

        _chatHistory = chatHistory;
        _agentThread = new ChatHistoryAgentThread(_chatHistory);
        _sharedTools = new SharedTools();
        _cancellationTokenSource = new CancellationTokenSource();

        var shellPlugin = new ShellPlugin(context);
        var textEditorPlugin = new TextEditorPlugin(context);

        var promptTemplateConfig = new PromptTemplateConfig
        {
            Template = EmbeddedResource.Read("Agent.Prompts.Instructions.txt"),
            TemplateFormat = "handlebars",
        };

        // Inject the plugins for the agent so it can interact with the environment.
        agentKernel.Plugins.AddFromObject(_sharedTools);
        agentKernel.Plugins.AddFromObject(shellPlugin);
        agentKernel.Plugins.AddFromObject(textEditorPlugin);

        var promptExecutionSettings = new AzureOpenAIPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        _agent = new ChatCompletionAgent(promptTemplateConfig, new HandlebarsPromptTemplateFactory())
        {
            Kernel = agentKernel,
            Arguments = new KernelArguments(promptExecutionSettings)
            {
                ["current_date"] = context.CurrentDateTime,
                ["operating_system"] = context.OperatingSystem,
                ["current_directory"] = context.WorkingDirectory,
            },
        };
    }

    public bool Running => _running;

    /// <summary>
    /// Invoke the agent with a prompt to start performing work.
    /// </summary>
    /// <param name="prompt">User prompt to process.</param>
    /// <param name="callbacks">The input/output interface for the agent.</param>
    /// <returns>Returns a stream of agent responses.</returns>
    public async Task<ChatHistory> InvokeAsync(string prompt, ICodingAgentCallbacks callbacks)
    {
        // Make sure to reset the final tool output before starting new work.
        // The final tool output is called when the agent is ready, and we don't want it to stop early.
        _sharedTools.ResetFinalToolOutput();
        _chatHistory.AddUserMessage(prompt);

        var iteration = 0;

        try
        {
            _running = true;

            while (iteration < MaxIterations)
            {
                var outputBuilder = new StringBuilder();

                // Check if the agent signaled that it is done. Stop the iteration loop if it is.
                // Signal the user we're done with the task.
                if (_sharedTools.FinalToolOutputReady)
                {
                    await callbacks.AgentCompleted(DateTime.Now);
                    return _chatHistory;
                }

                var invocationOptions = new AgentInvokeOptions
                {
                    OnIntermediateMessage = async (content) =>
                    {
                        //TODO: Report status    
                    }
                };
                
                var responseStream = _agent
                    .InvokeStreamingAsync(_agentThread, invocationOptions)
                    .WithCancellation(_cancellationTokenSource.Token)
                    .ConfigureAwait(false);
                
                await foreach (var chunk in responseStream)
                {
                    if (chunk.Message.Content is not null)
                    {
                        outputBuilder.Append(chunk.Message.Content);       
                    }
                }

                await callbacks.ReceiveAgentResponse(outputBuilder.ToString(), DateTime.Now);
                
                iteration++;
            }

            // Stop the process when cancellation is requested.
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                await callbacks.RequestCancelled(DateTime.Now);
                return _chatHistory;
            }

            if (iteration == MaxIterations && !_sharedTools.FinalToolOutputReady)
            {
                await callbacks.MaxIterationsReached(DateTime.Now);
            }
        }
        finally
        {
            _running = false;
        }
        
        return _chatHistory;
    }

    public void CancelRequest()
    {
        if (!_cancellationTokenSource.IsCancellationRequested && _running)
        {
            _cancellationTokenSource.Cancel();
        }
    }
}