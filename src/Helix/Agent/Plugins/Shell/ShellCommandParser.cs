namespace Helix.Agent.Plugins.Shell;

/// <summary>
/// Parses shell commands to extract executable names and arguments for allow list validation.
/// </summary>
public static class ShellCommandParser
{
    /// <summary>
    /// Parses a bash/Unix shell command string and extracts all commands.
    /// </summary>
    /// <param name="command">The bash command string to parse.</param>
    /// <returns>A list of parsed commands with their executables and arguments.</returns>
    public static List<ParsedCommand> ParseBashCommand(string command)
    {
        return ParseCommand(command, isBash: true);
    }

    /// <summary>
    /// Parses a Windows cmd.exe command string and extracts all commands.
    /// </summary>
    /// <param name="command">The Windows command string to parse.</param>
    /// <returns>A list of parsed commands with their executables and arguments.</returns>
    public static List<ParsedCommand> ParseWindowsCommand(string command)
    {
        return ParseCommand(command, isBash: false);
    }

    private static List<ParsedCommand> ParseCommand(string command, bool isBash)
    {
        var commands = new List<ParsedCommand>();

        if (string.IsNullOrWhiteSpace(command))
        {
            return commands;
        }

        // Split by command separators while preserving quoted strings
        var segments = SplitByCommandSeparators(command, isBash);

        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Extract executable and arguments
            var (executable, arguments) = ExtractExecutableAndArguments(trimmed);

            if (!string.IsNullOrWhiteSpace(executable))
            {
                commands.Add(new ParsedCommand(executable, arguments, trimmed));
            }
        }

        return commands;
    }

    private static List<string> SplitByCommandSeparators(string command, bool isBash)
    {
        var segments = new List<string>();
        var currentSegment = new System.Text.StringBuilder();
        var i = 0;
        var inSingleQuote = false;
        var inDoubleQuote = false;

        while (i < command.Length)
        {
            var ch = command[i];

            // Handle escape sequences
            if (ch == '\\' && i + 1 < command.Length)
            {
                if (isBash || inDoubleQuote)
                {
                    // In bash or within double quotes, backslash escapes the next character
                    currentSegment.Append(ch);
                    currentSegment.Append(command[i + 1]);
                    i += 2;
                    continue;
                }
            }

            // Handle quotes
            if (ch == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                currentSegment.Append(ch);
                i++;
                continue;
            }

            if (ch == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                currentSegment.Append(ch);
                i++;
                continue;
            }

            // Check for command separators (only when not in quotes)
            if (!inSingleQuote && !inDoubleQuote)
            {
                // Check for two-character separators first
                if (i + 1 < command.Length)
                {
                    var twoChar = command.Substring(i, 2);
                    if (twoChar == "&&" || twoChar == "||")
                    {
                        segments.Add(currentSegment.ToString());
                        currentSegment.Clear();
                        i += 2;
                        continue;
                    }
                }

                // Check for single-character separators
                // Note: & is a separator in both bash and Windows cmd.exe
                if (ch == '|' || ch == ';' || ch == '&')
                {
                    segments.Add(currentSegment.ToString());
                    currentSegment.Clear();
                    i++;
                    continue;
                }
            }

            currentSegment.Append(ch);
            i++;
        }

        // Add the last segment
        if (currentSegment.Length > 0)
        {
            segments.Add(currentSegment.ToString());
        }

        return segments;
    }

    private static (string executable, string arguments) ExtractExecutableAndArguments(string commandSegment)
    {
        // Remove leading redirections like "2>&1 command"
        commandSegment = commandSegment.TrimStart();

        var tokens = TokenizeCommand(commandSegment);

        if (tokens.Count == 0)
        {
            return (string.Empty, string.Empty);
        }

        // Find the first token that looks like an executable (not a redirection or assignment)
        var executableIndex = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // Skip variable assignments (VAR=value) and redirections (>, <, >>, 2>&1, etc.)
            if (token.Contains('=') && !token.StartsWith('/') && !token.StartsWith('.'))
            {
                continue;
            }

            if (token.StartsWith('>') || token.StartsWith('<') ||
                token.All(c => char.IsDigit(c) || c == '>' || c == '&'))
            {
                continue;
            }

            executableIndex = i;
            break;
        }

        var executable = tokens[executableIndex];
        var arguments = string.Join(" ", tokens.Skip(executableIndex + 1));

        // Clean up executable (remove quotes if present)
        executable = RemoveQuotes(executable);

        return (executable, arguments);
    }

    private static List<string> TokenizeCommand(string command)
    {
        var tokens = new List<string>();
        var currentToken = new System.Text.StringBuilder();
        var i = 0;
        var inSingleQuote = false;
        var inDoubleQuote = false;

        while (i < command.Length)
        {
            var ch = command[i];

            // Handle escape sequences
            if (ch == '\\' && i + 1 < command.Length && !inSingleQuote)
            {
                currentToken.Append(ch);
                currentToken.Append(command[i + 1]);
                i += 2;
                continue;
            }

            // Handle quotes
            if (ch == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                currentToken.Append(ch);
                i++;
                continue;
            }

            if (ch == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                currentToken.Append(ch);
                i++;
                continue;
            }

            // Handle whitespace (token separator when not in quotes)
            if (char.IsWhiteSpace(ch) && !inSingleQuote && !inDoubleQuote)
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
                i++;
                continue;
            }

            currentToken.Append(ch);
            i++;
        }

        // Add the last token
        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        return tokens;
    }

    private static string RemoveQuotes(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        // Remove surrounding quotes if they match
        if ((str.StartsWith("'") && str.EndsWith("'")) ||
            (str.StartsWith("\"") && str.EndsWith("\"")))
        {
            if (str.Length >= 2)
            {
                return str.Substring(1, str.Length - 2);
            }
        }

        return str;
    }
}
