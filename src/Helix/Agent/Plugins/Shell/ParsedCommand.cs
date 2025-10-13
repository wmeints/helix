namespace Helix.Agent.Plugins.Shell;

/// <summary>
/// Represents a parsed shell command with its executable name and arguments.
/// </summary>
/// <param name="Executable">The name of the executable to be run.</param>
/// <param name="Arguments">The arguments passed to the executable.</param>
/// <param name="FullCommand">The complete command string including executable and arguments.</param>
public record ParsedCommand(string Executable, string Arguments, string FullCommand);
