using Helix.Agent;
using Helix.Data;
using Helix.Hubs;
using Helix.Models;
using Helix.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace Helix.Tests;

public class CodingAgentHubTests
{
    private readonly Mock<ICodingAgentFactory> _mockFactory;
    private readonly Mock<IConversationRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICodingAgentCallbacks> _mockCaller;
    private readonly TestCodingAgentHub _hub;

    public CodingAgentHubTests()
    {
        _mockFactory = new Mock<ICodingAgentFactory>();
        _mockRepository = new Mock<IConversationRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCaller = new Mock<ICodingAgentCallbacks>();

        _hub = new TestCodingAgentHub(
            _mockFactory.Object,
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockCaller.Object);
    }

    private static Kernel CreateTestKernel()
    {
        var mockChatCompletionService = new Mock<IChatCompletionService>();
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(mockChatCompletionService.Object);
        return builder.Build();
    }

    private static CodingAgentContext CreateTestContext()
    {
        return new CodingAgentContext
        {
            TargetDirectory = "/test",
            OperatingSystem = "Linux",
            CurrentDateTime = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task SubmitPrompt_ShouldCreateNewConversation_WhenConversationDoesNotExist()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userPrompt = "Test prompt";
        var newConversation = new Conversation
        {
            Id = conversationId,
            Topic = "New Conversation",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync((Conversation?)null);

        _mockRepository
            .Setup(r => r.InsertConversationAsync(conversationId))
            .ReturnsAsync(newConversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), newConversation, CreateTestContext());
        mockAgent.Setup(a => a.SubmitPromptAsync(userPrompt, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(It.IsAny<Conversation>()))
            .Returns(mockAgent.Object);

        // Act
        await _hub.SubmitPrompt(conversationId, userPrompt);

        // Assert
        _mockRepository.Verify(r => r.FindByIdAsync(conversationId), Times.Once);
        _mockRepository.Verify(r => r.InsertConversationAsync(conversationId), Times.Once);
        _mockFactory.Verify(f => f.Create(It.IsAny<Conversation>()), Times.Once);
        mockAgent.Verify(a => a.SubmitPromptAsync(userPrompt, _mockCaller.Object), Times.Once);
        _mockRepository.Verify(r => r.UpdateConversationAsync(It.IsAny<Conversation>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitPrompt_ShouldUseExistingConversation_WhenConversationExists()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userPrompt = "Test prompt";
        var existingConversation = new Conversation
        {
            Id = conversationId,
            Topic = "Existing Conversation",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync(existingConversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), existingConversation, CreateTestContext());
        mockAgent.Setup(a => a.SubmitPromptAsync(userPrompt, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(existingConversation))
            .Returns(mockAgent.Object);

        // Act
        await _hub.SubmitPrompt(conversationId, userPrompt);

        // Assert
        _mockRepository.Verify(r => r.FindByIdAsync(conversationId), Times.Once);
        _mockRepository.Verify(r => r.InsertConversationAsync(It.IsAny<Guid>()), Times.Never);
        _mockFactory.Verify(f => f.Create(existingConversation), Times.Once);
        mockAgent.Verify(a => a.SubmitPromptAsync(userPrompt, _mockCaller.Object), Times.Once);
        _mockRepository.Verify(r => r.UpdateConversationAsync(existingConversation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitPrompt_ShouldInvokeAgentWithPrompt()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userPrompt = "Write a function to calculate factorial";
        var conversation = new Conversation
        {
            Id = conversationId,
            Topic = "Test",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), conversation, CreateTestContext());
        mockAgent.Setup(a => a.SubmitPromptAsync(userPrompt, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(conversation))
            .Returns(mockAgent.Object);

        // Act
        await _hub.SubmitPrompt(conversationId, userPrompt);

        // Assert
        mockAgent.Verify(a => a.SubmitPromptAsync(userPrompt, _mockCaller.Object), Times.Once);
    }

    [Fact]
    public async Task ApproveToolCall_ShouldThrowException_WhenConversationNotFound()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var toolCallId = "call_123";

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync((Conversation?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _hub.ApproveToolCall(conversationId, toolCallId));

        Assert.Contains($"No conversation found for id {conversationId}", exception.Message);
    }

    [Fact]
    public async Task ApproveToolCall_ShouldInvokeAgentAndSaveChanges_WhenConversationExists()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var toolCallId = "call_123";
        var conversation = new Conversation
        {
            Id = conversationId,
            Topic = "Test",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), conversation, CreateTestContext());
        mockAgent.Setup(a => a.ApproveFunctionCall(toolCallId, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(conversation))
            .Returns(mockAgent.Object);

        // Act
        await _hub.ApproveToolCall(conversationId, toolCallId);

        // Assert
        _mockRepository.Verify(r => r.FindByIdAsync(conversationId), Times.Once);
        _mockFactory.Verify(f => f.Create(conversation), Times.Once);
        mockAgent.Verify(a => a.ApproveFunctionCall(toolCallId, _mockCaller.Object), Times.Once);
        _mockRepository.Verify(r => r.UpdateConversationAsync(conversation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeclineToolCall_ShouldThrowException_WhenConversationNotFound()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var toolCallId = "call_456";

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync((Conversation?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _hub.DeclineToolCall(conversationId, toolCallId));

        Assert.Contains($"No conversation found for id {conversationId}", exception.Message);
    }

    [Fact]
    public async Task DeclineToolCall_ShouldInvokeAgentAndSaveChanges_WhenConversationExists()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var toolCallId = "call_456";
        var conversation = new Conversation
        {
            Id = conversationId,
            Topic = "Test",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), conversation, CreateTestContext());
        mockAgent.Setup(a => a.DeclineFunctionCall(toolCallId, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(conversation))
            .Returns(mockAgent.Object);

        // Act
        await _hub.DeclineToolCall(conversationId, toolCallId);

        // Assert
        _mockRepository.Verify(r => r.FindByIdAsync(conversationId), Times.Once);
        _mockFactory.Verify(f => f.Create(conversation), Times.Once);
        mockAgent.Verify(a => a.DeclineFunctionCall(toolCallId, _mockCaller.Object), Times.Once);
        _mockRepository.Verify(r => r.UpdateConversationAsync(conversation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitPrompt_ShouldUseFactoryToCreateAgent()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userPrompt = "Test prompt";
        var conversation = new Conversation
        {
            Id = conversationId,
            Topic = "Test",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), conversation, CreateTestContext());
        mockAgent.Setup(a => a.SubmitPromptAsync(userPrompt, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(conversation))
            .Returns(mockAgent.Object);

        // Act
        await _hub.SubmitPrompt(conversationId, userPrompt);

        // Assert - Verify factory was used to create the agent with the correct conversation
        _mockFactory.Verify(f => f.Create(conversation), Times.Once);
    }

    [Fact]
    public async Task ApproveToolCall_ShouldUseFactoryToCreateAgent()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var toolCallId = "call_789";
        var conversation = new Conversation
        {
            Id = conversationId,
            Topic = "Test",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), conversation, CreateTestContext());
        mockAgent.Setup(a => a.ApproveFunctionCall(toolCallId, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(conversation))
            .Returns(mockAgent.Object);

        // Act
        await _hub.ApproveToolCall(conversationId, toolCallId);

        // Assert - Verify factory was used to create the agent with the correct conversation
        _mockFactory.Verify(f => f.Create(conversation), Times.Once);
    }

    [Fact]
    public async Task DeclineToolCall_ShouldUseFactoryToCreateAgent()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var toolCallId = "call_999";
        var conversation = new Conversation
        {
            Id = conversationId,
            Topic = "Test",
            ChatHistory = new ChatHistory(),
            DateCreated = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FindByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        var mockAgent = new Mock<CodingAgent>(CreateTestKernel(), conversation, CreateTestContext());
        mockAgent.Setup(a => a.DeclineFunctionCall(toolCallId, _mockCaller.Object))
            .Returns(Task.CompletedTask);

        _mockFactory
            .Setup(f => f.Create(conversation))
            .Returns(mockAgent.Object);

        // Act
        await _hub.DeclineToolCall(conversationId, toolCallId);

        // Assert - Verify factory was used to create the agent with the correct conversation
        _mockFactory.Verify(f => f.Create(conversation), Times.Once);
    }

    /// <summary>
    /// Test hub that allows setting the caller for testing
    /// </summary>
    private class TestCodingAgentHub : CodingAgentHub
    {
        private readonly ICodingAgentCallbacks _testCaller;

        public TestCodingAgentHub(
            ICodingAgentFactory codingAgentFactory,
            IConversationRepository conversationRepository,
            IUnitOfWork unitOfWork,
            ICodingAgentCallbacks testCaller)
            : base(codingAgentFactory, conversationRepository, unitOfWork)
        {
            _testCaller = testCaller;
        }

        protected override ICodingAgentCallbacks GetAgentCallbacks()
        {
            return _testCaller;
        }
    }
}
