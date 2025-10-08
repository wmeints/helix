using System.Diagnostics;

namespace Helix.Plugins.Shell;

/// <summary>
/// A unix-based shell implementation for MacOS and Linux
/// </summary>
public class UnixShell: IShell
{
    /// <summary>
    /// Execute a shell command and returns the standard output and standard error as a tuple.
    /// </summary>
    /// <param name="command">Command to execute in the shell</param>
    /// <returns>Returns a tuple containing the STDOUT, and STDERR stream content</returns>
    public async Task<(string, string)> ExecuteCommandAsync(string command)
    {
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
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        
        foreach(var (key, value) in environmentVariables)
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