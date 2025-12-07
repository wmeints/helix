# Solution Strategy

## Technology Decisions

### Local LLM with Ollama

Helix uses [Ollama](https://ollama.ai/) as the LLM backend instead of cloud-based
APIs. This decision provides several benefits:

- **Privacy**: Code never leaves the developer's machine
- **Cost**: No per-token charges or API subscriptions
- **Flexibility**: Easy to switch between different models (currently uses
  qwen3-coder)
- **Offline capability**: Works without internet connectivity

### Terminal-Based Interface

The agent runs as a CLI application with a Rich-based terminal UI rather than a
web interface or IDE plugin. This approach:

- **Fits developer workflows**: Developers already work in terminals
- **Minimal dependencies**: No browser or GUI framework required
- **Cross-platform**: Works on any system with a terminal
- **Resource efficient**: Lower memory footprint than Electron-based tools

### LangGraph State Machine

The agent is built using [LangGraph](https://langchain-ai.github.io/langgraph/),
which provides:

- **Structured execution flow**: Clear state transitions between LLM calls and
  tool executions
- **Interrupt handling**: Built-in support for pausing execution to request user
  approval
- **Checkpointing**: State persistence for conversation continuity
- **Tool integration**: Seamless binding of Python functions as agent tools

### Interrupt-Based Tool Approval

All tool calls go through an approval flow using LangGraph's interrupt mechanism.
This ensures:

- **User control**: No file modifications or commands run without explicit consent
- **Transparency**: Users see exactly what the agent wants to do before execution
- **Permission rules**: Users can configure allow/deny rules to auto-approve
  trusted operations

## Key Design Patterns

### Cyclic Graph Pattern

The agent uses a cyclic graph structure where the LLM can repeatedly call tools
and process results:

```text
__start__ -> call_llm -> check_tool_approval -> call_tool -> call_llm (loop)
                      -> __end__
```

This pattern enables multi-step reasoning and iterative problem solving.

### Message Accumulation

State uses LangGraph's `add_messages` annotation to accumulate messages over
time, maintaining conversation context without manual list management.

### Separation of Concerns

- **Graph definition** handles execution flow
- **Tools** encapsulate specific capabilities
- **GUI** handles user interaction and rendering
- **Settings** manage permissions independently
