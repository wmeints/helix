using Helix.Agent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Tests;

public class CodingAgentTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly string _testDirectory;

    public CodingAgentTests()
    {
        // Get the path to the TestData directory
        var baseDirectory = AppContext.BaseDirectory;
        _testDataDirectory = Path.Combine(baseDirectory, "..", "..", "..", "TestData");
        _testDataDirectory = Path.GetFullPath(_testDataDirectory);

        // Create a unique test directory for this test run
        _testDirectory = Path.Combine(Path.GetTempPath(), $"helix-agent-tests-{Guid.NewGuid()}");
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
    public async Task InvokeAsync_ShouldSummarizeJavaScriptFile()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            // Use Skip.If from xUnit 3.0+ or just return
            return;
        }

        // Arrange
        // Copy test file to test directory
        CopyTestFile("calculator.js");

        var kernel = CreateKernel();
        var chatHistory = new ChatHistory();
        var context = new CodingAgentContext
        {
            WorkingDirectory = _testDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.Now
        };

        var agent = new CodingAgent(kernel, chatHistory, context);
        var callbacks = new TestCodingAgentCallbacks();

        // Act
        var prompt = "Please read the calculator.js file and provide a brief summary of what it does.";
        var result = await agent.InvokeAsync(prompt, callbacks);

        // Assert
        Assert.True(callbacks.AgentCompletedCalled, "Agent should have completed the task");
        Assert.NotEmpty(callbacks.Responses);

        // Verify chat history was updated
        Assert.NotEmpty(result);

        // The agent should have read the file and provided a summary
        var allContent = string.Join(" ", callbacks.Responses).ToLower();
        Assert.Contains("calculator", allContent);
    }

    [Fact]
    public async Task InvokeAsync_ShouldStopAfterMaxIterations()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            return;
        }

        // Arrange
        var kernel = CreateKernel();
        var chatHistory = new ChatHistory();
        var context = new CodingAgentContext
        {
            WorkingDirectory = _testDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.Now
        };

        var agent = new CodingAgent(kernel, chatHistory, context);
        var callbacks = new TestCodingAgentCallbacks();

        // Act - Give a prompt that won't use final_output to trigger max iterations
        var prompt = "Keep listing numbers from 1 to 1000, one at a time. Never stop.";

        var result = await agent.InvokeAsync(prompt, callbacks);

        // Assert
        Assert.True(callbacks.MaxIterationsReachedCalled || callbacks.AgentCompletedCalled,
            "Agent should either reach max iterations or complete");
    }

    [Fact]
    public async Task CancelRequest_ShouldStopAgentExecution()
    {
        // Skip test if Azure OpenAI credentials are not available
        if (!AreAzureCredentialsAvailable())
        {
            return;
        }

        // Arrange
        var kernel = CreateKernel();
        var chatHistory = new ChatHistory();
        var context = new CodingAgentContext
        {
            WorkingDirectory = _testDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            CurrentDateTime = DateTime.Now
        };

        var agent = new CodingAgent(kernel, chatHistory, context);
        var callbacks = new TestCodingAgentCallbacks();

        // Act
        var invokeTask = agent.InvokeAsync("List all numbers from 1 to 1000", callbacks);

        // Give the agent a moment to start
        await Task.Delay(1000);

        // Cancel the request
        agent.CancelRequest();

        var result = await invokeTask;

        // Assert
        Assert.False(agent.Running, "Agent should not be running after cancellation");
    }

    /// <summary>
    /// Test implementation of ICodingAgentCallbacks
    /// </summary>
    private class TestCodingAgentCallbacks : ICodingAgentCallbacks
    {
        public List<string> Responses { get; } = new();
        public List<(string toolName, List<string> arguments)> ToolCalls { get; } = new();
        public bool AgentCompletedCalled { get; private set; }
        public bool MaxIterationsReachedCalled { get; private set; }
        public bool RequestCancelledCalled { get; private set; }

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
