using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.SemanticKernel;

namespace Helix.Agent.Plugins.Shell;

/// <summary>
/// Coding agent plugin used to interact with the system shell.
/// </summary>
public class ShellPlugin
{
    /// <summary>
    /// Execute a shell command on behalf of the user.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <returns>The combined STDOUT and STDERR output</returns>
    [KernelFunction("shell")]
    [Description(
        "You can use the shell tool to execute any command. It can be used to solve a wide range of problems."
    )]
    public async Task<string> ExecuteCommandAsync([Description("Shell command to execute")] string command)
    {
        var shell = CreateShellInterface();
        var (stdout, stderr) = await shell.ExecuteCommandAsync(command);

        return $"{stdout}\n{stderr}";
    }

    /// <summary>
    /// Creates the shell interface for the plugin
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private IShell CreateShellInterface()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsShell();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new UnixShell();
        }

        throw new InvalidOperationException(
            $"Your operating system is not currently supported: {RuntimeInformation.OSDescription}");
    }
}