using Helix.Shared;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Helix.Agent;

/// <summary>
/// Manages agent instructions including system and custom instructions from the project directory hierarchy.
/// </summary>
public class AgentInstructions : IAgentInstructions
{
    private const string InstructionsFileName = "AGENTS.md";
    private const string CustomInstructionsAuthorName = "AGENTS_INSTRUCTIONS";
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentInstructions"/> class.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance used for rendering templates.</param>
    public AgentInstructions(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Injects system and custom instructions into the chat history.
    /// </summary>
    /// <param name="chatHistory">The chat history to inject instructions into.</param>
    /// <param name="context">The agent context containing environment information.</param>
    public async Task InjectAsync(ChatHistory chatHistory, CodingAgentContext context)
    {
        // Render and inject the system prompt as the first message in the chat history
        var systemPrompt = await ReadSystemInstructionsAsync(context);
        chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, systemPrompt));

        // Read and inject custom instructions from AGENTS.md if available
        var customInstructions = await ReadCustomInstructionsAsync(context.TargetDirectory);
        if (!string.IsNullOrWhiteSpace(customInstructions))
        {
            var instructionsMessage = new ChatMessageContent(AuthorRole.User, customInstructions)
            {
                AuthorName = CustomInstructionsAuthorName
            };
            chatHistory.Insert(1, instructionsMessage);
        }
    }

    /// <summary>
    /// Removes system and custom instructions from the chat history.
    /// </summary>
    /// <param name="chatHistory">The chat history to remove instructions from.</param>
    public void Remove(ChatHistory chatHistory)
    {
        // Remove the system prompt from the chat history
        if (chatHistory.Count > 0 && chatHistory[0].Role == AuthorRole.System)
        {
            chatHistory.RemoveAt(0);
        }

        // Remove custom instructions from the chat history
        var instructionsMessage = chatHistory
            .FirstOrDefault(m => m.AuthorName == CustomInstructionsAuthorName);

        if (instructionsMessage != null)
        {
            chatHistory.Remove(instructionsMessage);
        }
    }

    /// <summary>
    /// Reads and renders the system instructions using the provided context.
    /// </summary>
    /// <param name="context">The agent context containing environment information.</param>
    /// <returns>The rendered system instructions.</returns>
    private async Task<string> ReadSystemInstructionsAsync(CodingAgentContext context)
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

        return await promptTemplate.RenderAsync(_kernel, new KernelArguments
        {
            ["working_directory"] = context.TargetDirectory,
            ["current_date_time"] = context.CurrentDateTime,
            ["operating_system"] = context.OperatingSystem
        });
    }

    /// <summary>
    /// Reads custom instructions from an AGENTS.md file.
    /// Searches the target directory and parent directories for the file.
    /// </summary>
    /// <param name="targetDirectory">The directory to start searching from.</param>
    /// <returns>The content of the AGENTS.md file if found, otherwise null.</returns>
    private async Task<string?> ReadCustomInstructionsAsync(string targetDirectory)
    {
        var currentDirectory = new DirectoryInfo(targetDirectory);

        while (currentDirectory != null)
        {
            var instructionsPath = Path.Combine(currentDirectory.FullName, InstructionsFileName);

            if (File.Exists(instructionsPath))
            {
                return await File.ReadAllTextAsync(instructionsPath);
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }
}
