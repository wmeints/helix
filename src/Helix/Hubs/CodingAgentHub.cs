using Helix.Agent;
using Helix.Data;
using Helix.Models;
using Helix.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Hubs;

public class CodingAgentHub: Hub<ICodingAgentCallbacks>
{
    private readonly Kernel _applicationKernel;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CodingAgentHub(Kernel applicationKernel, IConversationRepository conversationRepository, IUnitOfWork unitOfWork)
    {
        _applicationKernel = applicationKernel;
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task SubmitPrompt(Guid conversationId, string userPrompt)
    {
        var conversation = await FindOrCreateConversationAsync(conversationId);

        var codingAgentContext = new CodingAgentContext
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
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