using Helix.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Tests;

public class ConversationInfoTests
{
    [Fact]
    public void FromConversation_WithEmptyChatHistory_ShouldReturnEmptyMessages()
    {
        // Arrange
        var chatHistory = new ChatHistory();
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(conversation.Id, result.Id);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void FromConversation_WithUserMessage_ShouldReturnUserPromptMessage()
    {
        // Arrange
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("Hello, how are you?");

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.IsType<UserPromptMessage>(result.Messages[0]);

        var userMessage = (UserPromptMessage)result.Messages[0];
        Assert.Equal("Hello, how are you?", userMessage.Content);
        Assert.Equal("UserPrompt", userMessage.MessageType);
    }

    [Fact]
    public void FromConversation_WithAssistantMessageOnly_ShouldReturnAgentResponseMessage()
    {
        // Arrange
        var chatHistory = new ChatHistory();
        chatHistory.AddAssistantMessage("I'm doing well, thank you!");

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.IsType<AgentResponseMessage>(result.Messages[0]);

        var agentMessage = (AgentResponseMessage)result.Messages[0];
        Assert.Equal("I'm doing well, thank you!", agentMessage.Content);
        Assert.Equal("AgentResponse", agentMessage.MessageType);
    }

    [Fact]
    public void FromConversation_WithToolCall_ShouldReturnToolCallMessage()
    {
        // Arrange
        var chatHistory = new ChatHistory();

        // Add an assistant message with a FunctionCallContent
        var functionCall = new FunctionCallContent("view_file", "plugin", "call_123",
            new KernelArguments { ["file_path"] = "/test/file.txt" });

        var assistantMessage = new ChatMessageContent(AuthorRole.Assistant, content: string.Empty);
        assistantMessage.Items.Add(functionCall);
        chatHistory.Add(assistantMessage);

        // Add the tool response
        chatHistory.Add(new ChatMessageContent(AuthorRole.Tool, "File content: Hello World"));

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.IsType<ToolCallMessage>(result.Messages[0]);

        var toolCallMessage = (ToolCallMessage)result.Messages[0];
        Assert.Equal("view_file", toolCallMessage.ToolName);
        Assert.Contains("file_path", toolCallMessage.Arguments);
        Assert.Equal("File content: Hello World", toolCallMessage.Response);
        Assert.Equal("ToolCall", toolCallMessage.MessageType);
    }

    [Fact]
    public void FromConversation_WithToolCallWithoutResponse_ShouldReturnToolCallMessageWithEmptyResponse()
    {
        // Arrange
        var chatHistory = new ChatHistory();

        // Add an assistant message with a FunctionCallContent but no following tool message
        var functionCall = new FunctionCallContent("view_file", "plugin", "call_123",
            new KernelArguments { ["file_path"] = "/test/file.txt" });

        var assistantMessage = new ChatMessageContent(AuthorRole.Assistant, content: string.Empty);
        assistantMessage.Items.Add(functionCall);
        chatHistory.Add(assistantMessage);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.IsType<ToolCallMessage>(result.Messages[0]);

        var toolCallMessage = (ToolCallMessage)result.Messages[0];
        Assert.Equal("view_file", toolCallMessage.ToolName);
        Assert.Equal(string.Empty, toolCallMessage.Response);
    }

    [Fact]
    public void FromConversation_WithMixedMessages_ShouldReturnAllMessageTypesInCorrectOrder()
    {
        // Arrange
        var chatHistory = new ChatHistory();

        // User message
        chatHistory.AddUserMessage("Can you read the file?");

        // Tool call with response
        var functionCall = new FunctionCallContent("view_file", "plugin", "call_123",
            new KernelArguments { ["file_path"] = "/test/file.txt" });
        var assistantMessage = new ChatMessageContent(AuthorRole.Assistant, content: string.Empty);
        assistantMessage.Items.Add(functionCall);
        chatHistory.Add(assistantMessage);
        chatHistory.Add(new ChatMessageContent(AuthorRole.Tool, "File content: Hello World"));

        // Agent response
        chatHistory.AddAssistantMessage("The file contains 'Hello World'");

        // Another user message
        chatHistory.AddUserMessage("Thank you!");

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Messages.Count);

        // First message: User prompt
        Assert.IsType<UserPromptMessage>(result.Messages[0]);
        Assert.Equal("Can you read the file?", ((UserPromptMessage)result.Messages[0]).Content);

        // Second message: Tool call
        Assert.IsType<ToolCallMessage>(result.Messages[1]);
        Assert.Equal("view_file", ((ToolCallMessage)result.Messages[1]).ToolName);
        Assert.Equal("File content: Hello World", ((ToolCallMessage)result.Messages[1]).Response);

        // Third message: Agent response
        Assert.IsType<AgentResponseMessage>(result.Messages[2]);
        Assert.Equal("The file contains 'Hello World'", ((AgentResponseMessage)result.Messages[2]).Content);

        // Fourth message: User prompt
        Assert.IsType<UserPromptMessage>(result.Messages[3]);
        Assert.Equal("Thank you!", ((UserPromptMessage)result.Messages[3]).Content);
    }

    [Fact]
    public void FromConversation_WithMultipleToolCalls_ShouldReturnAllToolCallMessages()
    {
        // Arrange
        var chatHistory = new ChatHistory();

        // First tool call
        var functionCall1 = new FunctionCallContent("view_file", "plugin", "call_123",
            new KernelArguments { ["file_path"] = "/test/file1.txt" });
        
        var assistantMessage1 = new ChatMessageContent(AuthorRole.Assistant, content: string.Empty);
        assistantMessage1.Items.Add(functionCall1);
        
        chatHistory.Add(assistantMessage1);
        chatHistory.Add(new ChatMessageContent(AuthorRole.Tool, "Content of file1"));

        // Second tool call
        var functionCall2 = new FunctionCallContent("view_file", "plugin", "call_124",
            new KernelArguments { ["file_path"] = "/test/file2.txt" });
        
        var assistantMessage2 = new ChatMessageContent(AuthorRole.Assistant, content: string.Empty);
        assistantMessage2.Items.Add(functionCall2);
        
        chatHistory.Add(assistantMessage2);
        chatHistory.Add(new ChatMessageContent(AuthorRole.Tool, "Content of file2"));

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);

        Assert.IsType<ToolCallMessage>(result.Messages[0]);
        Assert.Equal("Content of file1", ((ToolCallMessage)result.Messages[0]).Response);

        Assert.IsType<ToolCallMessage>(result.Messages[1]);
        Assert.Equal("Content of file2", ((ToolCallMessage)result.Messages[1]).Response);
    }

    [Fact]
    public void FromConversation_WithAssistantMessageContainingBothContentAndFunctionCall_ShouldTreatAsToolCall()
    {
        // Arrange
        var chatHistory = new ChatHistory();

        // Assistant message with both content and function call (function call should take precedence)
        var functionCall = new FunctionCallContent("execute_command", "plugin", "call_123",
            new KernelArguments { ["command"] = "ls -la" });

        var assistantMessage = new ChatMessageContent(AuthorRole.Assistant, "Let me execute that command");
        assistantMessage.Items.Add(functionCall);
        
        chatHistory.Add(assistantMessage);
        chatHistory.Add(new ChatMessageContent(AuthorRole.Tool, "Command output: file1.txt file2.txt"));

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Messages);

        // Should be treated as a tool call, not an agent response
        Assert.IsType<ToolCallMessage>(result.Messages[0]);
        Assert.Equal("execute_command", ((ToolCallMessage)result.Messages[0]).ToolName);
    }

    [Fact]
    public void FromConversation_WithComplexScenario_ShouldParseCorrectly()
    {
        // Arrange
        var chatHistory = new ChatHistory();

        // User asks to create a file
        chatHistory.AddUserMessage("Create a new file called test.txt with 'Hello' as content");

        // Agent calls write_file tool
        var writeFileCall = new FunctionCallContent("write_file", "plugin", "call_1",
            new KernelArguments { ["file_path"] = "test.txt", ["content"] = "Hello" });
        
        var assistantMessage1 = new ChatMessageContent(AuthorRole.Assistant, content: string.Empty);
        assistantMessage1.Items.Add(writeFileCall);
        
        chatHistory.Add(assistantMessage1);
        chatHistory.Add(new ChatMessageContent(AuthorRole.Tool, "File created successfully"));

        // Agent responds to user
        chatHistory.AddAssistantMessage("I've created the file test.txt with the content 'Hello'");

        // User asks to view it
        chatHistory.AddUserMessage("Now show me the contents");

        // Agent calls view_file tool
        var viewFileCall = new FunctionCallContent("view_file", "plugin", "call_2",
            new KernelArguments { ["file_path"] = "test.txt" });
        
        var assistantMessage2 = new ChatMessageContent(AuthorRole.Assistant, content: string.Empty);
        assistantMessage2.Items.Add(viewFileCall);
        
        chatHistory.Add(assistantMessage2);
        chatHistory.Add(new ChatMessageContent(AuthorRole.Tool, "Hello"));

        // Agent provides final response
        chatHistory.AddAssistantMessage("The file contains: Hello");

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "File Operations",
            ChatHistory = chatHistory,
            DateCreated = DateTime.UtcNow
        };

        // Act
        var result = ConversationInfo.FromConversation(conversation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.Messages.Count);

        Assert.IsType<UserPromptMessage>(result.Messages[0]);
        Assert.IsType<ToolCallMessage>(result.Messages[1]);
        Assert.Equal("write_file", ((ToolCallMessage)result.Messages[1]).ToolName);

        Assert.IsType<AgentResponseMessage>(result.Messages[2]);
        Assert.IsType<UserPromptMessage>(result.Messages[3]);

        Assert.IsType<ToolCallMessage>(result.Messages[4]);
        Assert.Equal("view_file", ((ToolCallMessage)result.Messages[4]).ToolName);

        Assert.IsType<AgentResponseMessage>(result.Messages[5]);
    }
}
