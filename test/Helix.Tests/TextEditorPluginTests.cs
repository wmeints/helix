using Helix.Agent;
using Helix.Agent.Plugins.TextEditor;

namespace Helix.Tests;

public class TextEditorPluginTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly TextEditorPlugin _plugin;

    public TextEditorPluginTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"helix-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        var codingAgentContext = new CodingAgentContext
        {
            TargetDirectory = _testDirectory,
            OperatingSystem = Environment.OSVersion.Platform.ToString()
        };
        
        _plugin = new TextEditorPlugin(codingAgentContext);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ViewFile_ShouldReadEntireFile_WhenToIsNegativeOne()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _plugin.ViewFile(testFile, 1, -1);

        // Assert
        Assert.Contains("Line 1", result);
        Assert.Contains("Line 2", result);
        Assert.Contains("Line 3", result);
        Assert.Contains("Line 4", result);
        Assert.Contains("Line 5", result);
    }

    [Fact]
    public async Task ViewFile_ShouldReadSpecificRange_WhenFromAndToAreProvided()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _plugin.ViewFile(testFile, 2, 4);

        // Assert
        Assert.Contains("Line 2", result);
        Assert.Contains("Line 3", result);
        Assert.Contains("Line 4", result);
        Assert.DoesNotContain("Line 1", result);
        Assert.DoesNotContain("Line 5", result);
    }

    [Fact]
    public async Task ViewFile_ShouldReadSingleLine_WhenFromAndToAreSame()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var content = "Line 1\nLine 2\nLine 3";
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _plugin.ViewFile(testFile, 2, 2);

        // Assert
        Assert.Contains("Line 2", result);
        Assert.DoesNotContain("Line 1", result);
        Assert.DoesNotContain("Line 3", result);
    }

    [Fact]
    public async Task ViewFile_ShouldClampToEnd_WhenToExceedsFileLength()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var content = "Line 1\nLine 2\nLine 3";
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _plugin.ViewFile(testFile, 1, 1000);

        // Assert
        Assert.Contains("Line 1", result);
        Assert.Contains("Line 2", result);
        Assert.Contains("Line 3", result);
    }

    [Fact]
    public async Task WriteFile_ShouldCreateNewFile_WithGivenContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "newfile.txt");
        var content = "Hello, World!";

        // Act
        await _plugin.WriteFile(testFile, content);

        // Assert
        Assert.True(File.Exists(testFile));
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(content, actualContent);
    }

    [Fact]
    public async Task WriteFile_ShouldOverwriteExistingFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "existing.txt");
        await File.WriteAllTextAsync(testFile, "Old content");
        var newContent = "New content";

        // Act
        await _plugin.WriteFile(testFile, newContent);

        // Assert
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(newContent, actualContent);
    }

    [Fact]
    public async Task WriteFile_ShouldCreateEmptyFile_WhenContentIsEmpty()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "empty.txt");

        // Act
        await _plugin.WriteFile(testFile, string.Empty);

        // Assert
        Assert.True(File.Exists(testFile));
        var content = await File.ReadAllTextAsync(testFile);
        Assert.Empty(content);
    }

    [Fact]
    public async Task InsertText_ShouldInsertAtBeginning_WhenLineIsZero()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "insert.txt");
        await File.WriteAllTextAsync(testFile, "Line 1\nLine 2\nLine 3");

        // Act
        await _plugin.InsertText(testFile, 0, "New First Line");

        // Assert
        var lines = await File.ReadAllLinesAsync(testFile);
        Assert.Equal("New First Line", lines[0]);
        Assert.Equal("Line 1", lines[1]);
    }

    [Fact]
    public async Task InsertText_ShouldInsertAtEnd_WhenLineEqualsFileLength()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "insert.txt");
        var originalLines = new[] { "Line 1", "Line 2", "Line 3" };
        await File.WriteAllLinesAsync(testFile, originalLines);

        // Act
        await _plugin.InsertText(testFile, 3, "New Last Line");

        // Assert
        var lines = await File.ReadAllLinesAsync(testFile);
        Assert.Equal(4, lines.Length); // Original 3 + inserted line
        Assert.Equal("New Last Line", lines[3]);
    }

    [Fact]
    public async Task InsertText_ShouldInsertInMiddle_WhenLineIsInRange()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "insert.txt");
        await File.WriteAllTextAsync(testFile, "Line 1\nLine 2\nLine 3");

        // Act
        await _plugin.InsertText(testFile, 1, "Inserted Line");

        // Assert
        var lines = await File.ReadAllLinesAsync(testFile);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Inserted Line", lines[1]);
        Assert.Equal("Line 2", lines[2]);
    }

    [Fact]
    public async Task InsertText_ShouldClampToEnd_WhenLineExceedsFileLength()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "insert.txt");
        var originalLines = new[] { "Line 1", "Line 2" };
        await File.WriteAllLinesAsync(testFile, originalLines);

        // Act
        await _plugin.InsertText(testFile, 100, "New Line");

        // Assert
        var content = await File.ReadAllTextAsync(testFile);
        Assert.Contains("New Line", content);
    }

    [Fact]
    public async Task ReplaceText_ShouldReplaceUniqueText_Successfully()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace.txt");
        await File.WriteAllTextAsync(testFile, "Hello World\nThis is a test\nGoodbye World");

        // Act
        await _plugin.ReplaceText(testFile, "This is a test", "This is replaced");

        // Assert
        var content = await File.ReadAllTextAsync(testFile);
        Assert.Contains("This is replaced", content);
        Assert.DoesNotContain("This is a test", content);
    }

    [Fact]
    public async Task ReplaceText_ShouldThrowException_WhenTextIsNotUnique()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace.txt");
        await File.WriteAllTextAsync(testFile, "Hello World\nHello World\nGoodbye");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _plugin.ReplaceText(testFile, "Hello World", "Hi World")
        );

        Assert.Contains("not unique", exception.Message);
    }

    [Fact]
    public async Task ReplaceText_ShouldHandleMultilineReplacement()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace.txt");
        var originalContent = "Line 1\nLine 2\nLine 3\nLine 4";
        await File.WriteAllTextAsync(testFile, originalContent);

        // Act
        await _plugin.ReplaceText(testFile, "Line 2\\nLine 3", "Single Line");

        // Assert
        var content = await File.ReadAllTextAsync(testFile);
        Assert.Contains("Single Line", content);
        Assert.DoesNotContain("Line 2", content);
        Assert.DoesNotContain("Line 3", content);
    }

    [Fact]
    public async Task ReplaceText_ShouldHandleRegexPatterns()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace.txt");
        await File.WriteAllTextAsync(testFile, "Version: 1.0.0\nOther content");

        // Act
        await _plugin.ReplaceText(testFile, "Version: \\d+\\.\\d+\\.\\d+", "Version: 2.0.0");

        // Assert
        var content = await File.ReadAllTextAsync(testFile);
        Assert.Contains("Version: 2.0.0", content);
        Assert.DoesNotContain("Version: 1.0.0", content);
    }

    [Fact]
    public async Task ReplaceText_ShouldReplaceEmptyStringWithContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace.txt");
        await File.WriteAllTextAsync(testFile, "");

        // Act
        await _plugin.ReplaceText(testFile, "^$", "New Content");

        // Assert
        var content = await File.ReadAllTextAsync(testFile);
        Assert.Equal("New Content", content);
    }
}
