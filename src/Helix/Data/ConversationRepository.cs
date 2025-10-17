using Helix.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Realtime;

namespace Helix.Data;

public class ConversationRepository(ApplicationDbContext applicationDbContext) : IConversationRepository
{
    public async Task<Conversation> InsertConversationAsync(Guid conversationId)
    {
        var conversation = new Conversation
        {
            Id = conversationId
        };

        await applicationDbContext.AddAsync(conversation);

        return conversation;
    }

    public Task UpdateConversationAsync(Conversation conversation)
    {
        applicationDbContext.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task<Conversation?> FindByIdAsync(Guid conversationId)
    {
        return applicationDbContext.Conversations.SingleOrDefaultAsync(x => x.Id == conversationId);
    }

    public async Task<IEnumerable<Conversation>> FindAllAsync()
    {
        return await applicationDbContext.Conversations
            .OrderByDescending(x => x.DateCreated).ToListAsync();
    }
}