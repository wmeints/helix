# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Helix is a coding agent built with Python using LangGraph and LangChain. It uses a local Ollama model (qwen3-coder) for LLM inference.

## Commands

**Install dependencies:**
```bash
uv sync
```

**Run the CLI:**
```bash
uv run helix
```

**Run all tests:**
```bash
uv run pytest
```

**Run a single test:**
```bash
uv run pytest tests/test_agent.py::test_agent_writes_haiku_about_ai -v
```

## Architecture

The agent is built as a LangGraph state machine with a cyclic graph pattern:

```
__start__ -> call_llm -> [should_call_tools] -> call_tool -> call_llm (loop)
                                             -> __end__
```

Key components in `src/helix/agent/`:
- **graph.py**: Defines the LangGraph `StateGraph` with two nodes (`call_llm`, `call_tool`) and a conditional edge that routes based on whether tool calls are present
- **state.py**: Defines `InputState` and `State` dataclasses using LangGraph's `add_messages` annotation for message accumulation
- **tools.py**: Contains tool definitions using `@tool` decorator; tools are exported via `TOOLS` list

The CLI entry point (`work` command) is defined in `src/helix/cli.py`.

## Dependencies

- Uses `uv` for package management (build backend: `uv_build`)
- `langchain-ollama` for LLM integration with local Ollama models
- `langgraph` for state machine orchestration
- `rich` for terminal output
- Tests use `pytest` with `pytest-asyncio` for async test support
