# Quickstart

This guide will help you get Helix up and running on your machine.

## Prerequisites

Before installing Helix, you need the following:

- **[uv](https://github.com/astral-sh/uv)** - A fast Python package manager
- **[Ollama](https://ollama.ai/)** - Local LLM runtime for running models on your
  machine

## Installing Ollama and the Required Model

1. Install Ollama by following the instructions at
   [ollama.ai](https://ollama.ai/)

2. Start the Ollama service:

   ```bash
   ollama serve
   ```

3. Pull the required model:

   ```bash
   ollama pull qwen3-coder
   ```

## Installing Helix

Install Helix as a global tool using uv:

```bash
uv tool install git+https://github.com/wmeints/helix
```

## Running Helix

### Interactive Mode

Start Helix in interactive mode to have a conversation with the agent:

```bash
helix
```

You'll see a welcome banner with available commands. Type your prompt and press
Enter to interact with the agent.

### Single Prompt Mode

For one-off tasks, use the `-p` flag to send a single prompt:

```bash
helix -p "Explain the main function in this project"
```

## Built-in Commands

While in interactive mode, you can use the following commands:

| Command    | Description                        |
| ---------- | ---------------------------------- |
| `/clear`   | Clear the conversation history     |
| `/prompts` | List available custom prompts      |
| `/exit`    | Exit the application               |

## Next Steps

- [Custom Prompts](prompts.md) - Create reusable prompt templates
- [Settings](settings.md) - Configure permissions and customize behavior
- [Safety](safety.md) - Learn about safety precautions when using the agent
