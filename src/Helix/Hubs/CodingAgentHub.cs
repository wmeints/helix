using Helix.Agent;
using Helix.Data;
using Helix.Models;
using Helix.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Hubs;

/// <summary>
/// SignalR hub connecting the frontend to the coding agent.
/// </summary>
public class CodingAgentHub : Hub<ICodingAgentCallbacks>
{
    private readonly Kernel _applicationKernel;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<CodingAgentOptions> _codingAgentOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodingAgentHub"/> class.
    /// </summary>
    /// <param name="applicationKernel">Semantic kernel instance</param>
    /// <param name="conversationRepository">Repository to load/save data</param>
    /// <param name="unitOfWork">Unit of work for the agent</param>
    /// <param name="codingAgentOptions">Options for the coding agent</param>
    public CodingAgentHub(Kernel applicationKernel,
        IConversationRepository conversationRepository, IUnitOfWork unitOfWork,
        IOptions<CodingAgentOptions> codingAgentOptions)
    {
        _applicationKernel = applicationKernel;
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
        _codingAgentOptions = codingAgentOptions;
    }

    /// <summary>
    /// Submits a prompt to a conversation.
    /// </summary>
    /// <param name="conversationId">Identifier for the conversation</param>
    /// <param name="userPrompt">Prompt that you want submitted</param>
    /// <remarks>
    /// If the conversation does not exist, it creates a new one.
    /// </remarks>
    public async Task SubmitPrompt(Guid conversationId, string userPrompt)
    {
        var conversation = await FindOrCreateConversationAsync(conversationId);

        var codingAgentContext = new CodingAgentContext
        {
            TargetDirectory = _codingAgentOptions.Value.TargetDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.UtcNow
        };

        var codingAgent = new CodingAgent(_applicationKernel, conversation, codingAgentContext);
        await codingAgent.SubmitPromptAsync(userPrompt, Clients.Caller);

        await _conversationRepository.UpdateConversationAsync(conversation);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Approve the use of a tool call in a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier for the tool conversation.</param>
    /// <param name="toolCallId">The unique identifier for the tool call.</param>
    /// <exception cref="ArgumentException">Gets thrown when the conversation couldn't be found.</exception>
    public async Task ApproveToolCall(Guid conversationId, string toolCallId)
    {
        var conversation = await _conversationRepository.FindByIdAsync(conversationId);

        if (conversation is null)
        {
            throw new ArgumentException($"No conversation found for id {conversationId}");
        }

        var codingAgentContext = new CodingAgentContext
        {
            TargetDirectory = _codingAgentOptions.Value.TargetDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.UtcNow
        };

        var codingAgent = new CodingAgent(_applicationKernel, conversation, codingAgentContext);
        await codingAgent.ApproveFunctionCall(toolCallId, Clients.Caller);

        await _conversationRepository.UpdateConversationAsync(conversation);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Decline the use of a tool call in a conversation.
    /// </summary>
    /// <param name="conversationId">Unique identifier for the conversation.</param>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <exception cref="ArgumentException">Gets thrown when the conversation couldn't be found.</exception>
    public async Task DeclineToolCall(Guid conversationId, string toolCallId)
    {
        var conversation = await _conversationRepository.FindByIdAsync(conversationId);

        if (conversation is null)
        {
            throw new ArgumentException($"No conversation found for id {conversationId}");
        }

        var codingAgentContext = new CodingAgentContext
        {
            TargetDirectory = _codingAgentOptions.Value.TargetDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.UtcNow
        };

        var codingAgent = new CodingAgent(_applicationKernel, conversation, codingAgentContext);
        await codingAgent.DeclineFunctionCall(toolCallId, Clients.Caller);

        await _conversationRepository.UpdateConversationAsync(conversation);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<Conversation> FindOrCreateConversationAsync(Guid conversationId)
    {
        var conversation = await _conversationRepository.FindByIdAsync(conversationId);

        if (conversation == null)
        {
            conversation = await _conversationRepository.InsertConversationAsync(conversationId);
        }

        return conversation;
    }
}