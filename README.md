# Helix

A coding agent powered by local LLMs.

## Overview

Helix is a terminal-based coding assistant that runs entirely on your local
machine using [Ollama](https://ollama.ai/). It can read and write files, execute
shell commands, and help with various software engineering tasks.

## Prerequisites

- [uv](https://github.com/astral-sh/uv) - Python package manager
- [Ollama](https://ollama.ai/) - Local LLM runtime
- qwen3-coder model: `ollama pull qwen3-coder`

## Installation

```bash
uv tool install git+https://github.com/wmeints/helix
```

## Usage

**Interactive mode:**

```bash
helix [-p "Your prompt"]
```

Leave out the `-p` parameter if you want to work interactively with the agent.

**Single prompt mode:**

```bash
uv run helix -p "Your prompt here"
```

## Commands

| Command    | Description                    |
| ---------- | ------------------------------ |
| `/clear`   | Clear conversation history     |
| `/prompts` | List available custom prompts  |
| `/exit`    | Exit the application           |

## Documentation

- [Architecture Documentation](docs/architecture/README.md) - Detailed system
  architecture following the arc42 template

## Development

```bash
# Run tests
uv run pytest

# Check code quality
uv run ruff check

# Fix linting issues
uv run ruff check --fix
```

## License

See [LICENSE](LICENSE) for details.
