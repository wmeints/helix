using Helix.Agent;
using Helix.Data;
using Helix.Hubs;
using Helix.Models;
using Helix.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Tests;

public class CodingAgentHubTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly string _testDirectory;

    public CodingAgentHubTests()
    {
        // Get the path to the TestData directory
        var baseDirectory = AppContext.BaseDirectory;
        _testDataDirectory = Path.Combine(baseDirectory, "..", "..", "..", "TestData");
        _testDataDirectory = Path.GetFullPath(_testDataDirectory);

        // Create a unique test directory for this test run
        _testDirectory = Path.Combine(Path.GetTempPath(), $"helix-hub-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    /// <summary>
    /// Copies test data files from the TestData directory to the test directory
    /// </summary>
    /// <param name="fileName">The name of the file to copy</param>
    /// <returns>The path to the copied file in the test directory</returns>
    private string CopyTestFile(string fileName)
    {
        var sourceFile = Path.Combine(_testDataDirectory, fileName);
        var destinationFile = Path.Combine(_testDirectory, fileName);

        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Test data file not found: {sourceFile}");
        }

        File.Copy(sourceFile, destinationFile, overwrite: true);
        return destinationFile;
    }

    /// <summary>
    /// Helper method to check if Azure OpenAI credentials are available
    /// </summary>
    private bool AreAzureCredentialsAvailable()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

        return !string.IsNullOrEmpty(endpoint) &&
               !string.IsNullOrEmpty(key) &&
               !string.IsNullOrEmpty(deployment);
    }

    /// <summary>
    /// Creates a Kernel configured with Azure OpenAI
    /// </summary>
    private Kernel CreateKernel()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: deployment!,
            endpoint: endpoint!,
            apiKey: key!
        );

        return kernelBuilder.Build();
    }

    [Fact]
    public async Task SubmitPrompt_WithExistingConversation_ShouldUseExistingConversation()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            return;
        }

        // Arrange
        CopyTestFile("calculator.js");

        var conversationId = Guid.NewGuid();
        var existingConversation = new Conversation
        {
            Id = conversationId,
            Topic = "Test Conversation",
            ChatHistory = new ChatHistory()
        };

        var repository = new MockConversationRepository();
        repository.AddConversation(existingConversation);

        var unitOfWork = new MockUnitOfWork();
        var kernel = CreateKernel();

        // Change to test directory for the hub to work correctly
        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        try
        {
            var hub = new CodingAgentHub(kernel, repository, unitOfWork);
            var callbacks = new TestCodingAgentCallbacks(hub);

            // Act
            await hub.SubmitPrompt(conversationId, "Read the calculator.js file and summarize it.");

            // Assert
            Assert.Equal(1, repository.FindByIdCallCount);
            Assert.Equal(0, repository.InsertCallCount); // Should not insert since it exists
            Assert.True(callbacks.AgentCompletedCalled);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    [Fact]
    public async Task SubmitPrompt_WithNewConversation_ShouldCreateNewConversation()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            return;
        }

        // Arrange
        CopyTestFile("calculator.js");

        var conversationId = Guid.NewGuid();
        var repository = new MockConversationRepository();
        var unitOfWork = new MockUnitOfWork();
        var kernel = CreateKernel();

        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        try
        {
            var hub = new CodingAgentHub(kernel, repository, unitOfWork);
            var callbacks = new TestCodingAgentCallbacks(hub);

            // Act
            await hub.SubmitPrompt(conversationId, "Read the calculator.js file and summarize it.");

            // Assert
            Assert.Equal(1, repository.FindByIdCallCount);
            Assert.Equal(1, repository.InsertCallCount); // Should create new conversation
            Assert.NotNull(repository.GetConversation(conversationId));
            Assert.True(callbacks.AgentCompletedCalled);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    [Fact]
    public async Task SubmitPrompt_ShouldInvokeAgentWithPrompt()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            return;
        }

        // Arrange
        CopyTestFile("calculator.js");

        var conversationId = Guid.NewGuid();
        var repository = new MockConversationRepository();
        var unitOfWork = new MockUnitOfWork();
        var kernel = CreateKernel();

        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        try
        {
            var hub = new CodingAgentHub(kernel, repository, unitOfWork);
            var callbacks = new TestCodingAgentCallbacks(hub);

            // Act
            var prompt = "Read the calculator.js file and summarize it.";
            await hub.SubmitPrompt(conversationId, prompt);

            // Assert
            Assert.NotEmpty(callbacks.Responses);
            var allContent = string.Join(" ", callbacks.Responses).ToLower();
            Assert.Contains("calculator", allContent);
            Assert.True(callbacks.AgentCompletedCalled);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    [Fact]
    public async Task SubmitPrompt_ShouldUpdateChatHistoryInRepository()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            return;
        }

        // Arrange
        CopyTestFile("calculator.js");

        var conversationId = Guid.NewGuid();
        var repository = new MockConversationRepository();
        var unitOfWork = new MockUnitOfWork();
        var kernel = CreateKernel();

        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        try
        {
            var hub = new CodingAgentHub(kernel, repository, unitOfWork);
            var callbacks = new TestCodingAgentCallbacks(hub);

            // Act
            await hub.SubmitPrompt(conversationId, "Read the calculator.js file and summarize it.");

            // Assert
            Assert.Equal(1, repository.UpdateChatHistoryCallCount);
            var conversation = repository.GetConversation(conversationId);
            Assert.NotNull(conversation);
            Assert.NotEmpty(conversation.ChatHistory);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    [Fact]
    public async Task SubmitPrompt_ShouldSaveChangesViaUnitOfWork()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            return;
        }

        // Arrange
        CopyTestFile("calculator.js");

        var conversationId = Guid.NewGuid();
        var repository = new MockConversationRepository();
        var unitOfWork = new MockUnitOfWork();
        var kernel = CreateKernel();

        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        try
        {
            var hub = new CodingAgentHub(kernel, repository, unitOfWork);
            var callbacks = new TestCodingAgentCallbacks(hub);

            // Act
            await hub.SubmitPrompt(conversationId, "Read the calculator.js file and summarize it.");

            // Assert
            Assert.True(unitOfWork.SaveChangesAsyncCalled);
            Assert.Equal(1, unitOfWork.SaveChangesAsyncCallCount);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    /// <summary>
    /// Mock implementation of IConversationRepository for testing
    /// </summary>
    private class MockConversationRepository : IConversationRepository
    {
        private readonly Dictionary<Guid, Conversation> _conversations = new();

        public int FindByIdCallCount { get; private set; }
        public int InsertCallCount { get; private set; }
        public int UpdateChatHistoryCallCount { get; private set; }

        public void AddConversation(Conversation conversation)
        {
            _conversations[conversation.Id] = conversation;
        }

        public Conversation? GetConversation(Guid conversationId)
        {
            _conversations.TryGetValue(conversationId, out var conversation);
            return conversation;
        }

        public Task<Conversation?> FindByIdAsync(Guid conversationId)
        {
            FindByIdCallCount++;
            _conversations.TryGetValue(conversationId, out var conversation);
            return Task.FromResult(conversation);
        }

        public Task<Conversation> InsertConversationAsync(Guid conversationId)
        {
            InsertCallCount++;
            var conversation = new Conversation
            {
                Id = conversationId,
                Topic = string.Empty,
                ChatHistory = new ChatHistory()
            };
            _conversations[conversationId] = conversation;
            return Task.FromResult(conversation);
        }

        public Task UpdateChatHistoryAsync(Guid conversationId, ChatHistory chatHistory)
        {
            UpdateChatHistoryCallCount++;
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.ChatHistory = chatHistory;
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Mock implementation of IUnitOfWork for testing
    /// </summary>
    private class MockUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesAsyncCalled { get; private set; }
        public int SaveChangesAsyncCallCount { get; private set; }

        public Task SaveChangesAsync()
        {
            SaveChangesAsyncCalled = true;
            SaveChangesAsyncCallCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test implementation of ICodingAgentCallbacks that uses the hub's Clients.Caller
    /// </summary>
    private class TestCodingAgentCallbacks : ICodingAgentCallbacks
    {
        private readonly CodingAgentHub _hub;

        public List<string> Responses { get; } = new();
        public List<(string toolName, List<string> arguments)> ToolCalls { get; } = new();
        public bool AgentCompletedCalled { get; private set; }
        public bool MaxIterationsReachedCalled { get; private set; }
        public bool RequestCancelledCalled { get; private set; }

        public TestCodingAgentCallbacks(CodingAgentHub hub)
        {
            _hub = hub;
        }

        public Task ReceiveAgentResponse(string content, DateTime timestamp)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                Responses.Add(content);
            }
            return Task.CompletedTask;
        }

        public Task ReceiveToolCall(string toolName, List<string> arguments, DateTime timestamp)
        {
            ToolCalls.Add((toolName, arguments));
            return Task.CompletedTask;
        }

        public Task AgentCompleted(DateTime timestamp)
        {
            AgentCompletedCalled = true;
            return Task.CompletedTask;
        }

        public Task MaxIterationsReached(DateTime timestamp)
        {
            MaxIterationsReachedCalled = true;
            return Task.CompletedTask;
        }

        public Task RequestCancelled(DateTime timestamp)
        {
            RequestCancelledCalled = true;
            return Task.CompletedTask;
        }
    }
}
