using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Models;

public class ConversationInfo
{
    public Guid Id { get; set; }
    public List<Message> Messages { get; set; } = new();

    public static ConversationInfo FromConversation(Conversation conversation)
    {
        var messages = new List<Message>();
        var chatHistory = conversation.ChatHistory;

        for (int i = 0; i < chatHistory.Count; i++)
        {
            var message = chatHistory[i];

            if (message.Role == AuthorRole.User)
            {
                // Process user prompt
                messages.Add(new UserPromptMessage
                {
                    Content = message.Content ?? string.Empty
                });
            }
            else if (message.Role == AuthorRole.Assistant)
            {
                // Check if this is a tool call (has FunctionCallContent in Items)
                var functionCall = message.Items?.OfType<FunctionCallContent>().FirstOrDefault();

                if (functionCall != null)
                {
                    // This is a tool call - get the response from the next message
                    var toolResponse = string.Empty;

                    if (i + 1 < chatHistory.Count && chatHistory[i + 1].Role == AuthorRole.Tool)
                    {
                        toolResponse = chatHistory[i + 1].Content ?? string.Empty;
                        i++; // Skip the tool response message in the next iteration
                    }

                    // Serialize arguments to JSON
                    var argumentsJson = string.Empty;
                    if (functionCall.Arguments != null)
                    {
                        argumentsJson = JsonSerializer.Serialize(functionCall.Arguments);
                    }

                    messages.Add(new ToolCallMessage
                    {
                        ToolName = functionCall.FunctionName,
                        Arguments = argumentsJson,
                        Response = toolResponse
                    });
                }
                else
                {
                    // Regular agent response
                    messages.Add(new AgentResponseMessage
                    {
                        Content = message.Content ?? string.Empty
                    });
                }
            }
            // Skip Tool role messages as they're already processed with their corresponding tool calls
        }

        return new ConversationInfo
        {
            Id = conversation.Id,
            Messages = messages
        };
    }
}