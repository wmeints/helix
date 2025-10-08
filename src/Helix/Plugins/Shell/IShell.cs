namespace Helix.Plugins;

public interface IShell
{
    /// <summary>
    /// Executes a shell command and returns the standard output and standard error as a tuple.
    /// </summary>
    /// <param name="command">Command to execute in the shell</param>
    /// <returns>Returns a tuple containing the STDOUT, and STDERR stream content</returns>
    Task<(string, string)> ExecuteCommandAsync(string command);
}