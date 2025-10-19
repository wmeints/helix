using Azure;
using Helix.Agent.Plugins;
using Helix.Agent.Plugins.Shell;
using Helix.Agent.Plugins.TextEditor;
using Helix.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Polly;
using Polly.Retry;

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

    private readonly Kernel _agentKernel;
    private readonly SharedTools _sharedTools;
    private readonly ShellPlugin _shellPlugin;
    private readonly TextEditorPlugin _textEditorPlugin;
    private readonly Conversation _conversation;
    private readonly CodingAgentContext _context;
    private readonly ILogger<CodingAgent> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodingAgent"/>
    /// </summary>
    /// <param name="kernel">Kernel instance to use</param>
    /// <param name="conversation">Conversation that we're working in</param>
    /// <param name="context">Agent context to provide</param>
    /// <param name="logger">Logger instance for logging retry attempts and errors</param>
    public CodingAgent(Kernel kernel, Conversation conversation, CodingAgentContext context, ILogger<CodingAgent> logger)
    {
        _agentKernel = kernel.Clone();

        _sharedTools = new SharedTools();
        _conversation = conversation;
        _context = context;
        _logger = logger;

        _shellPlugin = new ShellPlugin(context);
        _textEditorPlugin = new TextEditorPlugin(context);

        // Inject the plugins for the agent so it can interact with the environment.
        _agentKernel.Plugins.AddFromObject(_sharedTools);
        _agentKernel.Plugins.AddFromObject(_shellPlugin);
        _agentKernel.Plugins.AddFromObject(_textEditorPlugin);

        // Initialize the retry pipeline once
        _retryPipeline = CreateRetryPipeline();
    }

    /// <summary>
    /// Invoke the agent with a prompt to start performing work.
    /// </summary>
    /// <param name="userPrompt">User prompt to process.</param>
    /// <param name="callbacks">The input/output interface for the agent.</param>
    /// <returns>Returns a stream of agent responses.</returns>
    public virtual async Task SubmitPromptAsync(string userPrompt, ICodingAgentCallbacks callbacks)
    {
        // Make sure to reset the final tool output before starting new work.
        // The final tool output is called when the agent is ready, and we don't want it to stop early.
        _sharedTools.ResetFinalToolOutput();

        var chatCompletionService = _agentKernel.GetRequiredService<IChatCompletionService>();
        _conversation.ChatHistory.AddUserMessage(userPrompt);

        // Execute the agent loop to process the prompt.
        await ExecuteAgentLoopAsync(callbacks, chatCompletionService);
    }

    /// <summary>
    /// Approve a pending function call.
    /// </summary>
    /// <param name="callId">Identifier for the call.</param>
    /// <param name="callbacks">The input/output interface for the agent.</param>
    public virtual async Task ApproveFunctionCall(string callId, ICodingAgentCallbacks callbacks)
    {
        var pendingFunctionCall = _conversation.PendingFunctionCalls.SingleOrDefault(x => x.FunctionCallId == callId);

        if (pendingFunctionCall is not null)
        {
            // Retrieve the pending function call information so we can run the function call.
            var functionCallMessage = _conversation.ChatHistory.Last();
            var functionCalls = FunctionCallContent.GetFunctionCalls(functionCallMessage).ToList();
            var relatedFunctionCall = functionCalls.Single(x => x.Id == callId);

            // Execute the approved function call and add the response to the chat history.
            var response = relatedFunctionCall.InvokeAsync(_agentKernel);
            _conversation.ChatHistory.Add(response.Result.ToChatMessage());

            // Remove the pending function call
            _conversation.PendingFunctionCalls.Remove(pendingFunctionCall);
        }

        // Continue the agent loop when there aren't any other pending function calls that need approval.
        if (!_conversation.PendingFunctionCalls.Any())
        {
            var chatCompletionService = _agentKernel.GetRequiredService<IChatCompletionService>();
            await ExecuteAgentLoopAsync(callbacks, chatCompletionService);
        }
    }

    /// <summary>
    /// Decline a function call.
    /// </summary>
    /// <param name="callId">Unique identifier for the call</param>
    /// <param name="callbacks">The input/output interface for the agent.</param>
    public virtual async Task DeclineFunctionCall(string callId, ICodingAgentCallbacks callbacks)
    {
        var pendingFunctionCall = _conversation.PendingFunctionCalls
            .SingleOrDefault(x => x.FunctionCallId == callId);

        if (pendingFunctionCall is not null)
        {
            // Retrieve the pending function call information so we can run the function call.
            var functionCallMessage = _conversation.ChatHistory.Last();
            var functionCalls = FunctionCallContent.GetFunctionCalls(functionCallMessage).ToList();
            var relatedFunctionCall = functionCalls.Single(x => x.Id == callId);

            // Create a synthetic result notifying the agent the user declined permission. 
            // This allows the agent to find alternatives or continue processing the task.
            var functionResultContent = new FunctionResultContent(relatedFunctionCall.FunctionName,
                relatedFunctionCall.PluginName, result: "Error: User declined permission to execute this function.");

            _conversation.ChatHistory.Add(functionResultContent.ToChatMessage());

            _conversation.PendingFunctionCalls.Remove(pendingFunctionCall);
        }

        // Continue the agent loop when there aren't any other pending function calls that need approval.
        if (!_conversation.PendingFunctionCalls.Any())
        {
            var chatCompletionService = _agentKernel.GetRequiredService<IChatCompletionService>();
            await ExecuteAgentLoopAsync(callbacks, chatCompletionService);
        }
    }

    private async Task ExecuteAgentLoopAsync(ICodingAgentCallbacks callbacks,
        IChatCompletionService chatCompletionService)
    {
        var iterations = 0;

        // Render the system prompt and add it as the first message in the chat history
        var systemPrompt = await RenderSystemPrompt(_context);
        _conversation.ChatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, systemPrompt));

        while (true)
        {
            iterations++;

            // Create the prompt execution settings for the agent.
            var promptExecutionSettings = new AzureOpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false),
            };

            var response = await _retryPipeline.ExecuteAsync(async cancellationToken =>
                await chatCompletionService.GetChatMessageContentAsync(
                    _conversation.ChatHistory, promptExecutionSettings, _agentKernel, cancellationToken));

            // Always add the response to the chat history.
            // We'll apply special processing after storing the response.
            _conversation.ChatHistory.Add(response);

            // Agents can return regular text messages, so we'll handle those first.
            // Text responses never include function calls.
            if (response.Content is not null)
            {
                await callbacks.ReceiveAgentResponse(response.Content);
                continue;
            }

            // Check for function calls and process them as needed.
            var functionCalls = FunctionCallContent.GetFunctionCalls(response).ToList();

            if (functionCalls.Any())
            {
                await ProcessFunctionCalls(functionCalls, callbacks);

                // Pause the agent for now, we'll resume when we get permission from the user.
                if (_conversation.PendingFunctionCalls.Any())
                {
                    // Remove the system prompt before exiting
                    RemoveSystemPromptFromHistory();
                    break;
                }

                // Check if we got a final_output call.
                // This is a signal tool to stop the processing.
                if (_sharedTools.FinalToolOutputReady)
                {
                    // Remove the system prompt before exiting
                    RemoveSystemPromptFromHistory();
                    await callbacks.AgentCompleted();
                    break;
                }
            }

            // Make sure we stop after we've reached the maximum number of iterations.
            // The user can choose to continue the conversation if needed.
            if (iterations > MaxIterations)
            {
                // Remove the system prompt before exiting
                RemoveSystemPromptFromHistory();
                await callbacks.MaxIterationsReached();
                break;
            }
        }
    }

    private async Task ProcessFunctionCalls(List<FunctionCallContent> functionCalls, ICodingAgentCallbacks callbacks)
    {
        foreach (var functionCall in functionCalls)
            // Check if we need permission to call a function and request the permission as needed.
            // Otherwise, just invoke the function call directly and put the output into the chat history.
            if (RequiresPermission(functionCall))
            {
                var pendingFunctionCall = PendingFunctionCall.FromFunctionCallContent(functionCall);

                _conversation.PendingFunctionCalls.Add(pendingFunctionCall);

                await callbacks.RequestPermission(
                    pendingFunctionCall.FunctionCallId,
                    pendingFunctionCall.FunctionName,
                    pendingFunctionCall.Arguments);
            }
            else
            {
                var output = await functionCall.InvokeAsync(_agentKernel);
                _conversation.ChatHistory.Add(output.ToChatMessage());

                // Report the results back to the user. The final_output tool is special and indicates the agent is done.
                // We aren't going to get a nice message from the agent when it calls final_output, so we mimic that the agent does that.
                if (functionCall.FunctionName == "final_output")
                {
                    await callbacks.ReceiveAgentResponse(functionCall.Arguments?["output"]?.ToString() ?? string.Empty);
                }
                else
                {
                    await callbacks.ReceiveToolCall(
                        functionCall.FunctionName,
                        ParseFunctionCallArguments(functionCall.Arguments));
                }
            }
    }

    private bool RequiresPermission(FunctionCallContent content)
    {
        if (_shellPlugin.RequiresPermission(content))
        {
            return true;
        }

        if (_textEditorPlugin.RequiresPermission(content))
        {
            return true;
        }

        return false;
    }

    private async Task<string> RenderSystemPrompt(CodingAgentContext context)
    {
        var promptTemplateConfig = new PromptTemplateConfig
        {
            Template = EmbeddedResource.Read("Agent.Prompts.Instructions.md"),
            TemplateFormat = "handlebars"
        };

        var promptTemplateFactory = new HandlebarsPromptTemplateFactory
        {
            AllowDangerouslySetContent = true
        };

        var promptTemplate = promptTemplateFactory.Create(promptTemplateConfig);

        return await promptTemplate.RenderAsync(_agentKernel, new KernelArguments
        {
            ["working_directory"] = context.TargetDirectory,
            ["current_date_time"] = context.CurrentDateTime,
            ["operating_system"] = context.OperatingSystem
        });
    }

    /// <summary>
    /// Removes the system prompt from the chat history.
    /// </summary>
    private void RemoveSystemPromptFromHistory()
    {
        if (_conversation.ChatHistory.Count > 0 &&
            _conversation.ChatHistory[0].Role == AuthorRole.System)
        {
            _conversation.ChatHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Creates a retry pipeline to handle rate limit errors from Azure OpenAI.
    /// </summary>
    /// <returns>A configured resilience pipeline with retry logic.</returns>
    private ResiliencePipeline CreateRetryPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex => ex.Status == 429),
                MaxRetryAttempts = 3,
                DelayGenerator = args =>
                {
                    // Use Retry-After header if available
                    if (args.Outcome.Exception is RequestFailedException reqEx &&
                        reqEx.GetRawResponse()?.Headers.TryGetValue("Retry-After", out var retryAfter) == true)
                    {
                        if (int.TryParse(retryAfter, out var retryAfterSeconds))
                        {
                            return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(retryAfterSeconds));
                        }
                    }

                    // Otherwise use exponential backoff
                    return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber + 1)));
                },
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Rate limit hit. Retrying attempt {AttemptNumber} after {DelaySeconds}s",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private Dictionary<string, string> ParseFunctionCallArguments(KernelArguments? arguments)
    {
        var parsedArguments = new Dictionary<string, string>();

        if (arguments is not null)
        {
            foreach (var key in arguments.Keys)
            {
                parsedArguments[key] = arguments[key]?.ToString() ?? string.Empty;
            }
        }

        return parsedArguments;
    }
}