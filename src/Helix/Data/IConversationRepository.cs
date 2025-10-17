using Helix.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Data;

public interface IConversationRepository
{
    Task<Conversation> InsertConversationAsync(Guid conversationId);
    Task UpdateConversationAsync(Conversation conversation);
    Task<Conversation?> FindByIdAsync(Guid conversationId);
    Task<IEnumerable<Conversation>> FindAllAsync();
}