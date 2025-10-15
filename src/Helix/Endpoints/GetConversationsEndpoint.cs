namespace Helix.Endpoints;

public static class GetConversationsEndpoint
{
    public static void MapGetConversations(this WebApplication app)
    {
        app.MapGet("/api/conversations", GetConversations);
        app.MapGet("/api/conversations/{id:guid}", GetConversationById);
    }

    public static async Task<IResult> GetConversations(IConversationRepository repository)
    {
        var conversations = await repository.FindAllAsync();

        var responseData = conversations
            .Select(x=> ConversationInfo.FromConversation(x))
            .ToList();

        return Results.Ok(responseData);
    }

    public static async Task<IResult> GetConversationById(Guid id, IConversationRepository repository)
    {
        var conversation = await repository.FindByIdAsync(id);

        if (conversation == null)
        {
            return Results.NotFound(new { message = $"Conversation with id {id} not found" });
        }

        var responseData = ConversationInfo.FromConversation(conversation);

        return Results.Ok(responseData);
    }
}