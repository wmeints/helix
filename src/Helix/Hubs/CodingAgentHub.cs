using Helix.Agent;
using Helix.Data;
using Helix.Models;
using Helix.Services;
using Microsoft.AspNetCore.SignalR;

namespace Helix.Hubs;

/// <summary>
/// SignalR hub connecting the frontend to the coding agent.
/// </summary>
public class CodingAgentHub : Hub<ICodingAgentCallbacks>
{
    private readonly ICodingAgentFactory _codingAgentFactory;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodingAgentHub"/> class.
    /// </summary>
    /// <param name="codingAgentFactory">Factory for creating coding agent instances</param>
    /// <param name="conversationRepository">Repository to load/save data</param>
    /// <param name="unitOfWork">Unit of work for the agent</param>
    public CodingAgentHub(ICodingAgentFactory codingAgentFactory,
        IConversationRepository conversationRepository, IUnitOfWork unitOfWork)
    {
        _codingAgentFactory = codingAgentFactory;
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
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

        var codingAgent = _codingAgentFactory.Create(conversation);
        await codingAgent.SubmitPromptAsync(userPrompt, GetAgentCallbacks());

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

        var codingAgent = _codingAgentFactory.Create(conversation);
        await codingAgent.ApproveFunctionCall(toolCallId, GetAgentCallbacks());

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

        var codingAgent = _codingAgentFactory.Create(conversation);
        await codingAgent.DeclineFunctionCall(toolCallId, GetAgentCallbacks());

        await _conversationRepository.UpdateConversationAsync(conversation);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the caller for the current hub connection. Can be overridden for testing.
    /// </summary>
    /// <returns>The caller callbacks interface.</returns>
    protected virtual ICodingAgentCallbacks GetAgentCallbacks()
    {
        return Clients.Caller;
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