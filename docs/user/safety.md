# Safety Precautions

Helix is a powerful coding agent that can read and write files, execute shell
commands, and modify your codebase. This guide covers important safety
considerations and best practices.

## Understanding the Risks

### File Operations

The agent can:

- **Read any file** in your project or system (subject to OS permissions)
- **Write to any file**, potentially overwriting existing content
- **Insert text** into existing files

### Shell Command Execution

The agent can execute arbitrary shell commands with your user permissions. This
includes commands that could:

- Delete files and directories
- Modify system configuration
- Install or remove software
- Access network resources
- Execute scripts

## Built-in Safety Features

### Tool Approval System

By default, Helix prompts you before executing any tool. When the agent wants to
use a tool, you'll see:

- The tool name (e.g., `write_file`, `run_shell_command`)
- The arguments (e.g., file path, command to execute)

You can then choose to:

- **Allow once** - Execute this specific action
- **Deny** - Block this action
- **Always allow** - Add a permission rule for similar actions

### Permission Rules

Use deny rules to block dangerous operations:

```json
{
  "permissions": {
    "deny": [
      "run_shell_command(rm:*)",
      "run_shell_command(sudo:*)",
      "run_shell_command(chmod:*)",
      "run_shell_command(curl:*)",
      "run_shell_command(wget:*)"
    ]
  }
}
```

See [Settings](settings.md) for more details on configuring permissions.

## Best Practices

### 1. Review Tool Calls Carefully

Always read the full command or file path before approving:

- Check file paths to ensure they're within your project
- Review shell commands for potentially destructive operations
- Be cautious with commands that access the network

### 2. Use Deny Rules for Dangerous Commands

Block commands that could cause harm:

```json
{
  "permissions": {
    "deny": [
      "run_shell_command(rm -rf:*)",
      "run_shell_command(dd:*)",
      "run_shell_command(mkfs:*)",
      "run_shell_command(:(){ :|:& };:)"
    ]
  }
}
```

### 3. Work in Version-Controlled Directories

Always use Helix in a Git repository so you can:

- Review changes with `git diff`
- Revert unwanted modifications with `git checkout` or `git reset`
- Track what the agent has modified

### 4. Limit Shell Command Permissions

Be selective about which shell commands to auto-approve:

```json
{
  "permissions": {
    "allow": [
      "run_shell_command(uv:*)",
      "run_shell_command(git status:*)",
      "run_shell_command(git diff:*)"
    ]
  }
}
```

Avoid blanket approvals like `run_shell_command:*` which would auto-approve any
command.

### 5. Use a Dedicated Environment

Consider running Helix in:

- A container or virtual machine for isolation
- A separate user account with limited permissions
- A sandboxed directory structure

### 6. Don't Store Secrets in Your Codebase

The agent can read any file it has access to. Ensure:

- Environment files (`.env`) with secrets are in `.gitignore`
- API keys and credentials are stored securely outside the project
- Sensitive configuration is not committed to the repository

### 7. Review Agent Output

After the agent completes a task:

- Check `git diff` to see all changes made
- Run your test suite to verify functionality
- Review any new files created

## Emergency Recovery

If the agent makes unwanted changes:

### Revert File Changes

```bash
# Discard all uncommitted changes
git checkout -- .

# Or revert specific files
git checkout -- path/to/file
```

### Stop a Running Command

Press `Ctrl+C` to interrupt the agent if a command is taking too long or appears
to be misbehaving.

### Clear Conversation and Start Fresh

Use `/clear` to reset the conversation history if the agent gets into a
problematic state.

## Reporting Issues

If you encounter safety issues or have suggestions for improving Helix's safety
features, please report them at:

https://github.com/wmeints/helix/issues

## Next Steps

- [Quickstart](quickstart.md) - Installation and basic usage
- [Custom Prompts](prompts.md) - Create reusable prompt templates
- [Settings](settings.md) - Configure permissions and customize behavior
