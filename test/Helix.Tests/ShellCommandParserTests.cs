using Helix.Agent.Plugins.Shell;

namespace Helix.Tests;

public class ShellCommandParserTests
{
    [Fact]
    public void ParseBashCommand_SimpleCommand_ShouldExtractExecutable()
    {
        // Arrange
        var command = "ls -la";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("ls", result[0].Executable);
        Assert.Equal("-la", result[0].Arguments);
        Assert.Equal("ls -la", result[0].FullCommand);
    }

    [Fact]
    public void ParseBashCommand_CommandWithMultipleArguments_ShouldExtractCorrectly()
    {
        // Arrange
        var command = "git commit -m \"test message\"";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("git", result[0].Executable);
        Assert.Equal("commit -m \"test message\"", result[0].Arguments);
    }

    [Fact]
    public void ParseBashCommand_PipedCommands_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "cat file.txt | grep pattern";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("cat", result[0].Executable);
        Assert.Equal("file.txt", result[0].Arguments);
        Assert.Equal("grep", result[1].Executable);
        Assert.Equal("pattern", result[1].Arguments);
    }

    [Fact]
    public void ParseBashCommand_ChainedCommandsWithAnd_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "cd /tmp && npm install";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("cd", result[0].Executable);
        Assert.Equal("/tmp", result[0].Arguments);
        Assert.Equal("npm", result[1].Executable);
        Assert.Equal("install", result[1].Arguments);
    }

    [Fact]
    public void ParseBashCommand_ChainedCommandsWithOr_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "command1 || command2";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("command1", result[0].Executable);
        Assert.Equal("command2", result[1].Executable);
    }

    [Fact]
    public void ParseBashCommand_ChainedCommandsWithSemicolon_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "echo hello; echo world";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("echo", result[0].Executable);
        Assert.Equal("hello", result[0].Arguments);
        Assert.Equal("echo", result[1].Executable);
        Assert.Equal("world", result[1].Arguments);
    }

    [Fact]
    public void ParseBashCommand_BackgroundCommand_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "long_running_task & quick_task";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("long_running_task", result[0].Executable);
        Assert.Equal("quick_task", result[1].Executable);
    }

    [Fact]
    public void ParseBashCommand_QuotedArguments_ShouldPreserveQuotes()
    {
        // Arrange
        var command = "echo 'hello world'";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("echo", result[0].Executable);
        Assert.Equal("'hello world'", result[0].Arguments);
    }

    [Fact]
    public void ParseBashCommand_QuotedExecutable_ShouldRemoveQuotes()
    {
        // Arrange
        var command = "\"my-command\" arg1 arg2";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("my-command", result[0].Executable);
        Assert.Equal("arg1 arg2", result[0].Arguments);
    }

    [Fact]
    public void ParseBashCommand_ComplexPipelineWithQuotes_ShouldExtractCorrectly()
    {
        // Arrange
        var command = "find . -name '*.txt' | xargs grep \"pattern\" | sort";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("find", result[0].Executable);
        Assert.Equal(". -name '*.txt'", result[0].Arguments);
        Assert.Equal("xargs", result[1].Executable);
        Assert.Equal("grep \"pattern\"", result[1].Arguments);
        Assert.Equal("sort", result[2].Executable);
    }

    [Fact]
    public void ParseBashCommand_CommandWithRedirection_ShouldExtractExecutable()
    {
        // Arrange
        var command = "echo hello > output.txt";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("echo", result[0].Executable);
        Assert.Contains("hello", result[0].Arguments);
    }

    [Fact]
    public void ParseBashCommand_EmptyCommand_ShouldReturnEmptyList()
    {
        // Arrange
        var command = "";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseBashCommand_WhitespaceOnly_ShouldReturnEmptyList()
    {
        // Arrange
        var command = "   \t\n  ";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseBashCommand_CommandWithEnvironmentVariable_ShouldExtractExecutable()
    {
        // Arrange
        var command = "PATH=/usr/bin ls -la";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("ls", result[0].Executable);
        Assert.Equal("-la", result[0].Arguments);
    }

    [Fact]
    public void ParseWindowsCommand_SimpleCommand_ShouldExtractExecutable()
    {
        // Arrange
        var command = "dir /s";

        // Act
        var result = ShellCommandParser.ParseWindowsCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("dir", result[0].Executable);
        Assert.Equal("/s", result[0].Arguments);
    }

    [Fact]
    public void ParseWindowsCommand_ChainedWithAmpersand_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "echo hello & echo world";

        // Act
        var result = ShellCommandParser.ParseWindowsCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("echo", result[0].Executable);
        Assert.Equal("hello", result[0].Arguments);
        Assert.Equal("echo", result[1].Executable);
        Assert.Equal("world", result[1].Arguments);
    }

    [Fact]
    public void ParseWindowsCommand_ChainedWithDoubleAmpersand_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "cd C:\\temp && dir";

        // Act
        var result = ShellCommandParser.ParseWindowsCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("cd", result[0].Executable);
        Assert.Equal("C:\\temp", result[0].Arguments);
        Assert.Equal("dir", result[1].Executable);
    }

    [Fact]
    public void ParseWindowsCommand_PipedCommands_ShouldExtractMultipleCommands()
    {
        // Arrange
        var command = "type file.txt | findstr pattern";

        // Act
        var result = ShellCommandParser.ParseWindowsCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("type", result[0].Executable);
        Assert.Equal("file.txt", result[0].Arguments);
        Assert.Equal("findstr", result[1].Executable);
        Assert.Equal("pattern", result[1].Arguments);
    }

    [Fact]
    public void ParseBashCommand_EscapedCharacters_ShouldPreserveEscapes()
    {
        // Arrange
        var command = "echo hello\\ world";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("echo", result[0].Executable);
        Assert.Contains("hello\\ world", result[0].Arguments);
    }

    [Fact]
    public void ParseBashCommand_MixedQuotesAndPipes_ShouldHandleCorrectly()
    {
        // Arrange
        var command = "echo 'hello | world' | grep world";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("echo", result[0].Executable);
        Assert.Equal("'hello | world'", result[0].Arguments);
        Assert.Equal("grep", result[1].Executable);
        Assert.Equal("world", result[1].Arguments);
    }

    [Fact]
    public void ParseBashCommand_ComplexRealWorldExample_ShouldExtractAllCommands()
    {
        // Arrange
        var command = "git add . && git commit -m \"Updated files\" && git push origin main";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("git", result[0].Executable);
        Assert.Equal("add .", result[0].Arguments);
        Assert.Equal("git", result[1].Executable);
        Assert.Equal("commit -m \"Updated files\"", result[1].Arguments);
        Assert.Equal("git", result[2].Executable);
        Assert.Equal("push origin main", result[2].Arguments);
    }

    [Fact]
    public void ParseBashCommand_DotnetCommand_ShouldExtractCorrectly()
    {
        // Arrange
        var command = "dotnet test --no-build --verbosity normal";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("dotnet", result[0].Executable);
        Assert.Equal("test --no-build --verbosity normal", result[0].Arguments);
    }

    [Fact]
    public void ParseBashCommand_NpmCommand_ShouldExtractCorrectly()
    {
        // Arrange
        var command = "npm install --save-dev typescript";

        // Act
        var result = ShellCommandParser.ParseBashCommand(command);

        // Assert
        Assert.Single(result);
        Assert.Equal("npm", result[0].Executable);
        Assert.Equal("install --save-dev typescript", result[0].Arguments);
    }
}
