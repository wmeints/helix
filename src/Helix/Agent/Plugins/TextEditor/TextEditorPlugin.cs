using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace Helix.Agent.Plugins.TextEditor;

/// <summary>
/// Provides various text manipulation tools to the coding agent.
/// </summary>
public class TextEditorPlugin(CodingAgentContext context)
{
    /// <summary>
    /// View the contents of a file.
    /// </summary>
    /// <param name="path">Path to the file to read.</param>
    /// <param name="from">The start line number (1-indexed)</param>
    /// <param name="to">The end line number (1-indexed)</param>
    /// <returns>Returns the relevant content from the file.</returns>
    [KernelFunction("view_file")]
    [Description("Use this tool to view the contents of a file.")]
    public async Task<string> ViewFile(
        [Description("The path of the file you want to view")] string path, 
        [Description("The start line number (1-indexed)")]int from,
        [Description("The end line number (1-indexed). Use -1 to read to the end of the file")] int to)
    {
        var fileLocation = FileLocation.Resolve(path);
        var lines = await File.ReadAllLinesAsync(fileLocation);

        var outputBuilder = new StringBuilder();

        to = to == -1 ? lines.Length : to;
        to = Math.Clamp(to, 1, lines.Length);
        
        for (int index = from - 1; index < to; index++)
        {
            outputBuilder.AppendLine(lines[index]);
        }
        
        return outputBuilder.ToString();
    }

    /// <summary>
    /// Write content to a new file
    /// </summary>
    /// <param name="path">Path to the file to read.</param>
    /// <param name="content">Content to write to the file.</param>
    [KernelFunction("write_file")]
    [Description("Use this tool to create a new file with the content you want. The file must not already exist.")]
    public async Task WriteFile(
        [Description("Path to the new file")] string path,
        [Description("The content you want in the new file")] string content)
    {
        var fileLocation = FileLocation.Resolve(path);
        await File.WriteAllTextAsync(fileLocation, content);
    }

    /// <summary>
    /// Inserts text content into an existing file
    /// </summary>
    /// <param name="path">Path to the existing file</param>
    /// <param name="line">Line number to insert content at</param>
    /// <param name="content">Content to insert into the file</param>
    [KernelFunction("insert_text")]
    [Description("Use this tool to insert text into an existing file at a specific line number.")]
    public async Task InsertText(
        [Description("The path to the existing file")] string path, 
        [Description("Line number to insert the text at. Use 0 to insert the text at the start of the file.")] int line, 
        [Description("Content to insert into the existing file")] string content)
    {
        var fileLocation = FileLocation.Resolve(path);
        var lines = await File.ReadAllLinesAsync(fileLocation);
        
        line = Math.Clamp(line, 0, lines.Length);
        var outputBuilder = new StringBuilder();
        
        for (int index = 0; index < lines.Length; index++)
        {
            if (index == line)
            {
                outputBuilder.AppendLine(content);
            }
            
            outputBuilder.AppendLine(lines[index]);
        }
        
        // If the line is at the end of the file, append it now.
        if (line == lines.Length)
        {
            outputBuilder.AppendLine(content);
        }
        
        await File.WriteAllTextAsync(fileLocation, outputBuilder.ToString());
    }

    /// <summary>
    /// Replace text in an existing file.
    /// </summary>
    /// <param name="path">Path to the existing file</param>
    /// <param name="oldText">Old text to replace</param>
    /// <param name="newText">New text to replace the old text with</param>
    [KernelFunction("replace_text")]
    [Description(
        """
        Use this tool to replace a piece of text in an existing file with new text. Make sure the 
        old text is unique in the file to avoid unintended replacements.
        """)]
    public async Task ReplaceText(
        [Description("Path to the existing file")]string path, 
        [Description("The original text to replace in the file")]string oldText, 
        [Description("The new text to replace the old text with")]string newText)
    {
        var fileLocation = FileLocation.Resolve(path);
        var fileContent = await File.ReadAllTextAsync(fileLocation);

        var matchPattern = new Regex(oldText, RegexOptions.Multiline);
        var matches = matchPattern.Matches(fileContent);

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                "The text to replace is not unique in the file. Aborting replacement to avoid unintended changes."
            );
        }
        
        var updatedContent = matchPattern.Replace(fileContent, newText);
        
        await File.WriteAllTextAsync(fileLocation, updatedContent);
    }
}