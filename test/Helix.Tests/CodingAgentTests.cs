using Helix.Agent;
using Helix.Agent.Plugins;
using Helix.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Moq;

namespace Helix.Tests;

public class CodingAgentTests
{
    [Fact]
    public async Task SubmitPromptAsync_ShouldProcessPromptAndComplete_WhenFinalOutputIsCalled()
    {
        // Arrange
        var mockChatCompletionService = new Mock<IChatCompletionService>();
        var mockCallbacks = new Mock<ICodingAgentCallbacks>();

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton(mockChatCompletionService.Object);
        var kernel = kernelBuilder.Build();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        var context = new CodingAgentContext
        {
            TargetDirectory = Directory.GetCurrentDirectory(),
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.UtcNow
        };

        var agent = new CodingAgent(kernel, conversation, context);

        // Create a function call content for final_output
        var functionCallContent = new FunctionCallContent(
            "final_output",
            nameof(SharedTools),
            "call_123",
            new KernelArguments { ["output"] = "Task completed successfully" }
        );

        var messageWithFunctionCall = new ChatMessageContent(
            AuthorRole.Assistant,
            new ChatMessageContentItemCollection { functionCallContent }
        );

        // Mock GetChatMessageContentsAsync (the actual interface method, not the extension)
        mockChatCompletionService
            .Setup(x => x.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageWithFunctionCall });

        // Act
        await agent.SubmitPromptAsync("Test prompt", mockCallbacks.Object);

        // Assert
        Assert.Equal(3, conversation.ChatHistory.Count); // User message + assistant message + function result
        Assert.Equal("Test prompt", conversation.ChatHistory[0].Content);
        Assert.Equal(AuthorRole.User, conversation.ChatHistory[0].Role);

        // Verify that AgentCompleted was called
        mockCallbacks.Verify(x => x.AgentCompleted(), Times.Once);
    }

    [Fact]
    public async Task SubmitPromptAsync_ShouldStopLoopAndRequestPermission_WhenToolRequiresPermission()
    {
        // Arrange
        var mockChatCompletionService = new Mock<IChatCompletionService>();
        var mockCallbacks = new Mock<ICodingAgentCallbacks>();

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton(mockChatCompletionService.Object);
        kernelBuilder.Services.AddSingleton<IPromptTemplateFactory>(new HandlebarsPromptTemplateFactory());
        var kernel = kernelBuilder.Build();

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Topic = "Test Conversation",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        var context = new CodingAgentContext
        {
            TargetDirectory = Directory.GetCurrentDirectory(),
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.UtcNow
        };

        var agent = new CodingAgent(kernel, conversation, context);

        // Create a function call content for a shell command (which requires permission)
        var functionCallContent = new FunctionCallContent(
            "shell",
            "ShellPlugin",
            "call_123",
            new KernelArguments { ["command"] = "ls -la" }
        );

        var messageWithFunctionCall = new ChatMessageContent(
            AuthorRole.Assistant,
            new ChatMessageContentItemCollection { functionCallContent }
        );

        // Mock GetChatMessageContentsAsync
        mockChatCompletionService
            .Setup(x => x.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageWithFunctionCall });

        // Act
        await agent.SubmitPromptAsync("Execute ls command", mockCallbacks.Object);

        // Assert
        Assert.Equal(2, conversation.ChatHistory.Count); // User message + assistant message with function call
        Assert.Equal("Execute ls command", conversation.ChatHistory[0].Content);
        Assert.Equal(AuthorRole.User, conversation.ChatHistory[0].Role);

        // Verify that the agent has a pending function call
        Assert.Single(conversation.PendingFunctionCalls);
        var pendingCall = conversation.PendingFunctionCalls[0];
        Assert.Equal("call_123", pendingCall.FunctionCallId);
        Assert.Equal("shell", pendingCall.FunctionName);

        // Verify that RequestPermission was called
        mockCallbacks.Verify(x => x.RequestPermission(
            "call_123",
            "shell",
            It.IsAny<Dictionary<string, string>>()), Times.Once);

        // Verify that AgentCompleted was NOT called (loop should have stopped)
        mockCallbacks.Verify(x => x.AgentCompleted(), Times.Never);
    }
}