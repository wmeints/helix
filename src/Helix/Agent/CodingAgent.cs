using System.Runtime.CompilerServices;
using System.Text;
using Helix.Agent.Plugins;
using Helix.Agent.Plugins.Shell;
using Helix.Agent.Plugins.TextEditor;
using Helix.Shared;
using Helix.Terminal;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Helix.Agent;

/// <summary>
/// The coding agent is the core of Helix. It connects the LLM to tools to complete coding tasks.
/// </summary>
public class CodingAgent
{
    private const int MaxIterations = 20;

    private CancellationTokenSource _cancellationTokenSource;
    private ChatHistoryAgent _agent;
    private ChatHistory _chatHistory;
    private ChatHistoryAgentThread _agentThread;
    private bool _running;
    private readonly SharedTools _sharedTools;

    public CodingAgent(Kernel kernel)
    {
        var agentKernel = kernel.Clone();

        _chatHistory = new ChatHistory();
        _agentThread = new ChatHistoryAgentThread(_chatHistory);
        _sharedTools = new SharedTools();
        _cancellationTokenSource = new CancellationTokenSource();

        var shellPlugin = new ShellPlugin();
        var textEditorPlugin = new TextEditorPlugin();

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
                ["current_date"] = DateTime.Now,
                ["operating_system"] = Environment.OSVersion.Platform.ToString(),
                ["current_directory"] = Environment.CurrentDirectory,
            },
        };
    }

    public bool Running => _running;

    /// <summary>
    /// Invoke the agent with a prompt to start performing work.
    /// </summary>
    /// <param name="prompt">User prompt to process.</param>
    /// <param name="agentInterface">The input/output interface for the agent.</param>
    /// <returns>Returns a stream of agent responses.</returns>
    public async Task InvokeAsync(string prompt, AgentInterface agentInterface)
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
                if (_sharedTools.FinalToolOutputReady)
                {
                    agentInterface.AppendMessage(_sharedTools.FinalToolOutputValue);
                    return;
                }

                var responseStream = _agent
                    .InvokeStreamingAsync(_agentThread).WithCancellation(_cancellationTokenSource.Token).ConfigureAwait(false);

                agentInterface.AppendRule();
                
                await foreach (var chunk in responseStream)
                {
                    if (chunk.Message.Content is not null)
                    {
                        agentInterface.AppendMessageFragment(chunk.Message.Content);    
                    }
                }

                agentInterface.CompleteMessage();

                iteration++;
            }

            // Stop the process when cancellation is requested.
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (iteration == MaxIterations && !_sharedTools.FinalToolOutputReady)
            {
                // Signal the user that we've not completed the work required and reached the maximum number
                // of iterations. The user can type additional commands to help the agent, or they can use one
                // of the slash commands to stop the agent completely.
                agentInterface.AppendMessage(
                    "The agent reached the maximum number of iterations. Do you want to continue iterating?");
            }
        }
        finally
        {
            _running = false;
        }
    }

    public void CancelRequest()
    {
        if (!_cancellationTokenSource.IsCancellationRequested && _running)
        {
            _cancellationTokenSource.Cancel();
        }
    }
}