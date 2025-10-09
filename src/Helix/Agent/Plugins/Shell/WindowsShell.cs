using System.Diagnostics;

namespace Helix.Agent.Plugins.Shell;

/// <summary>
/// A windows-based shell implementation
/// </summary>
public class WindowsShell : IShell
{
    /// <summary>
    /// Execute a shell command and returns the standard output and standard error as a tuple.
    /// </summary>
    /// <param name="command">Command to execute in the shell</param>
    /// <returns>Returns a tuple containing the STDOUT, and STDERR stream content</returns>
    public async Task<(string, string)> ExecuteCommandAsync(string command)
    {
        // Inject extra environment variables to disable various interactive tools for the agent.
        // We're also modifying the behavior slightly to make sure that paging is disabled for GIT tools.
        var environmentVariables = new Dictionary<string, string?>
        {
            ["EDITOR"] = "sh -c 'echo \\\"Interactive editor not available in this environment.\\\" >&2; exit 1'",
            ["VISUAL"] = "sh -c 'echo \\\"Interactive editor not available in this environment.\\\" >&2; exit 1'",
            ["GIT_PAGER"] = "cat",
            ["GIT_TERMINAL_PROMPT"] = "0",
            ["GIT_SEQUENCE_EDITOR"] = "sh -c 'echo \\\"Interactive Git commands are not supported in this environment.\\\" >&2; exit 1'",
            ["GIT_EDITOR"] = "sh -c 'echo \\\"Interactive Git commands are not supported in this environment.\\\" >&2; exit 1'",
        };

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var (key, value) in environmentVariables)
        {
            processStartInfo.Environment[key] = value;
        }

        var process = new Process
        {
            StartInfo = processStartInfo
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (output, error);
    }
}