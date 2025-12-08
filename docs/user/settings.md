# Settings

Helix uses a settings file to configure permissions and customize behavior. This
guide explains all available settings and how to configure them.

## Settings File Location

Settings are stored in `.helix/settings.json` in your project directory. If
this file doesn't exist, Helix uses default settings.

## Model Configuration

You can configure which LLM model to use and the context window size.

### Basic Structure

```json
{
  "model": "qwen3-coder",
  "context_window_size": 128000,
  "permissions": {
    "allow": [],
    "deny": []
  }
}
```

### Model

The `model` setting specifies which Ollama model to use. The default is
`qwen3-coder`. You can change this to any model available in your Ollama
installation:

```json
{
  "model": "llama3.1"
}
```

Make sure the model is pulled in Ollama before using it:

```bash
ollama pull llama3.1
```

### Context Window Size

The `context_window_size` setting controls how many tokens can fit in the
model's context window. The default is `128000` (128K tokens), which matches
the qwen3-coder model's context window.

When using a different model, adjust this to match its context window:

```json
{
  "model": "llama3.1",
  "context_window_size": 128000
}
```

Common context window sizes:

- qwen3-coder: 128,000 tokens
- llama3.1: 128,000 tokens
- gemma2: 8,192 tokens (8K) or 128,000 tokens (128K) depending on variant

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
  "model": "qwen3-coder",
  "context_window_size": 128000,
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

- Uses the qwen3-coder model with a 128K context window
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
