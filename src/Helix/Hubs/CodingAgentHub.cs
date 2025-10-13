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
public class CodingAgentHub: Hub<ICodingAgentCallbacks>
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
            OperatingSystem = Environment.OSVersion.Platform.ToString()
        };
        
        var codingAgent = new CodingAgent(_applicationKernel, conversation.ChatHistory, codingAgentContext);
        var updatedHistory = await codingAgent.InvokeAsync(userPrompt, Clients.Caller);
        
        await _conversationRepository.UpdateChatHistoryAsync(conversationId, updatedHistory);
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