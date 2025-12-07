# Building Blocks View

## Module Overview

```text
src/helix/
├── __init__.py          # Package initialization
├── cli.py               # CLI entry point (Click-based)
├── gui.py               # Terminal GUI (Rich-based)
├── prompts.py           # Custom prompt loading
├── settings.py          # Permission settings management
└── agent/
    ├── __init__.py      # Agent package exports
    ├── graph.py         # LangGraph state machine
    ├── state.py         # State dataclasses
    ├── tools.py         # Tool definitions
    └── system_instructions.md  # System prompt template
```

## Module Descriptions

### cli.py

The CLI entry point using the Click framework. Provides two modes:

- **Interactive mode**: Launches the terminal GUI for conversation
- **Single prompt mode**: Executes a single prompt via `-p` flag

### gui.py

Rich-based terminal interface responsible for:

- Rendering the welcome banner and user prompts
- Displaying agent responses with markdown formatting
- Showing tool calls and their results
- Handling the tool approval dialog
- Managing the interaction loop

### settings.py

Manages permission rules for tool execution:

- Loads settings from `.helix/settings.json`
- Supports allow/deny rules for tools
- Pattern matching for shell commands (e.g., `run_shell_command(uv:*)`)
- Persists user preferences when they choose "always allow"

### prompts.py

Handles custom prompt templates:

- Loads `.prompt.md` files from `.helix/prompts/`
- Supports variable substitution with `$ARGUMENTS`
- Enables user-defined shortcuts for common tasks

### agent/graph.py

Defines the LangGraph state machine with:

- **call_llm node**: Invokes the Ollama model with tool bindings
- **check_tool_approval node**: Interrupts for user approval
- **call_tool node**: Executes approved tools
- **Conditional edges**: Routes based on tool call presence and approval status

### agent/state.py

Defines state structures:

- **InputState**: Minimal input interface with message list
- **State**: Extended state for internal processing

Uses LangGraph's `add_messages` annotation for automatic message accumulation.

### agent/tools.py

Defines available tools:

| Tool              | Purpose                                      |
| ----------------- | -------------------------------------------- |
| read_file         | Read file contents with optional line range  |
| write_file        | Write content to a file                      |
| insert_text       | Insert text at a specific line               |
| run_shell_command | Execute shell commands (bash/cmd)            |
| write_todos       | Manage the agent's todo list                 |
| read_todos        | Read current todo items                      |

## Dependencies Between Modules

```text
cli.py
  └── gui.py
        ├── agent/graph.py
        │     ├── agent/state.py
        │     └── agent/tools.py
        ├── prompts.py
        └── settings.py
```
