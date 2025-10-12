using System.Runtime.InteropServices;
using Helix.Agent;
using Helix.Agent.Plugins.Shell;

namespace Helix.Tests;

public class ShellPluginTests
{
    [Fact]
    public async Task ExecuteCommandAsync_ShouldEchoString()
    {
        // Arrange
        var codingAgentContext = new CodingAgentContext
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            OperatingSystem = Environment.OSVersion.Platform.ToString()
        };
        
        var plugin = new ShellPlugin(codingAgentContext);
        var expectedOutput = "HelloWorld";

        // Act
        var command = $"echo {expectedOutput}";
        var result = await plugin.ExecuteCommandAsync(command);

        // Assert
        Assert.Contains(expectedOutput, result);
    }
}
