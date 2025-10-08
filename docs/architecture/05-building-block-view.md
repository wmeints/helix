# Building block view

This section covers the building blocks contained in the Helix agent.

## Shell extension

This extension allows the agent to execute shell commands on the user's computer.
It has the following tools available:

### execute_command

This allows the agent to run a shell command. On Windows we're using cmd.exe
to execute commands. On Linux we'll use bash as the shell to run the commands.

The `execute_command` requires the following parameters:

- **Command:** The command to execute on the user's system.

## Text Editor Extension

This extension allows the agent to manipulate files. It can use the following
tools from the extension:

### view_file

This allows the agent to view the contents of a file.
The tool requires the following parameters:

- **Path:** The path to the file that the agent wants to see
- **FromLineNumber:** The starting line number to read from the file (1-indexed)
- **ToLineNumber:** The ending line number to read from the file (1-indexed)

If the agent specifies -1 for the end line it means it wants to read until the
end of the file.

### write_file

This allows the agent to write content to a file. It requires the following
parameters:

- **Path**: The path to the file that the agent wants to write
- **FileContent:** The content to write into the file

### insert_text

This allows the agent to insert text into a file at a specific line number.
It requires the following parameters:

- **Path**: The path to the file that the agent wants to write.
- **LineNumber:** The line number to insert the text at. Use 0 to insert at the start of the file.
- **Content:** The content to write into the file.

### replace_text

This allows the agent to replace pieces of text in a file. It requires the following
parameters:

- **Path**: The path to the file that the agent wants to replace content in.
- **OldContent:** The text to replace in the file.
- **NewContent:** The text to replace the old content with.

