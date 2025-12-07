# Settings

Helix uses a settings file to configure permissions and customize behavior. This
guide explains all available settings and how to configure them.

## Settings File Location

Settings are stored in `.helix/settings.json` in your project directory. If
this file doesn't exist, Helix uses default settings.

## Permissions

The primary setting is `permissions`, which controls which tools the agent can
use without asking for approval.

### Basic Structure

```json
{
  "permissions": {
    "allow": [],
    "deny": []
  }
}
```

### How Permission Rules Work

When the agent wants to use a tool, Helix checks the permission rules in order:

1. If any **deny** rule matches, the tool call is blocked
2. If any **allow** rule matches, the tool call is automatically approved
3. If no rules match, the user is prompted to approve or deny the tool call

### Allow Rules

Allow rules let you automatically approve specific tools. There are two formats:

**Simple tool rules** - Allow a tool by name:

```json
{
  "permissions": {
    "allow": ["read_file", "write_file"]
  }
}
```

**Shell command patterns** - Allow shell commands matching a pattern:

```json
{
  "permissions": {
    "allow": ["run_shell_command(uv:*)"]
  }
}
```

The pattern syntax for shell commands:

- `uv` - Matches exactly the command `uv`
- `uv:*` - Matches `uv` and any command starting with `uv ` (e.g., `uv run`,
  `uv sync`)
- `uv run:*` - Matches `uv run` and any command starting with `uv run `

### Deny Rules

Deny rules block specific tools or commands. They use the same format as allow
rules:

```json
{
  "permissions": {
    "deny": ["run_shell_command(rm:*)"]
  }
}
```

This example blocks any shell command starting with `rm`.

### Rule Precedence

Deny rules are always checked first. If a tool call matches both an allow rule
and a deny rule, it will be **denied**.

### Example Configuration

```json
{
  "permissions": {
    "allow": [
      "read_file",
      "run_shell_command(uv:*)",
      "run_shell_command(git:*)"
    ],
    "deny": [
      "run_shell_command(rm:*)",
      "run_shell_command(sudo:*)"
    ]
  }
}
```

This configuration:

- Automatically approves reading files
- Automatically approves `uv` and `git` commands
- Blocks `rm` and `sudo` commands
- Prompts the user for all other tool calls

## Available Tools

Helix provides the following tools that can be configured with permissions:

| Tool                | Description                           |
| ------------------- | ------------------------------------- |
| `read_file`         | Read content from a file              |
| `write_file`        | Write content to a file               |
| `insert_text`       | Insert text at a specific line        |
| `run_shell_command` | Execute a shell command               |
| `write_todos`       | Update the agent's todo list          |
| `read_todos`        | Read the current todo list            |

## Adding Rules Interactively

When prompted to approve a tool call, you can choose:

- **yes (y)** - Allow this specific call once
- **no (n)** - Deny this specific call
- **always (a)** - Allow and add a permission rule for future calls

Choosing "always" automatically adds an allow rule to your settings file.

## Next Steps

- [Quickstart](quickstart.md) - Installation and basic usage
- [Custom Prompts](prompts.md) - Create reusable prompt templates
- [Safety](safety.md) - Learn about safety precautions when using the agent
