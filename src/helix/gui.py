"""Rich-based terminal GUI for the Helix coding agent."""

import asyncio
from typing import Any

from langchain_core.messages import AIMessage, HumanMessage, ToolMessage
from rich.console import Console
from rich.markdown import Markdown
from rich.panel import Panel
from rich.prompt import Prompt as RichPrompt
from rich.text import Text

from helix.agent.graph import THREAD_ID, clear_conversation, graph
from helix.prompts import Prompt, load_prompts

# Global console instance
console = Console()

# Built-in commands
EXIT_COMMAND = "/exit"
CLEAR_COMMAND = "/clear"
PROMPTS_COMMAND = "/prompts"

# Loaded custom prompts
_custom_prompts: dict[str, Prompt] = {}


def render_tool_call(tool_name: str, tool_args: dict[str, Any]) -> Panel:
    """
    Render a tool call with its parameters.

    Parameters
    ----------
    tool_name : str
        The name of the tool being called.
    tool_args : dict[str, Any]
        Dictionary of argument names to values.

    Returns
    -------
    Panel
        A Rich Panel displaying the tool call.
    """
    args_formatted = ", ".join(
        f"{key}={repr(value)}" for key, value in tool_args.items()
    )

    call_text = Text()
    call_text.append("Calling ", style="dim")
    call_text.append(tool_name, style="bold magenta")
    call_text.append("(", style="dim")
    call_text.append(args_formatted, style="cyan")
    call_text.append(")", style="dim")

    return Panel(
        call_text,
        title="[yellow]Tool Invocation[/yellow]",
        border_style="yellow",
        padding=(0, 1),
    )


def render_tool_result(tool_name: str, content: str) -> Panel:
    """
    Render the result of a tool execution (first 5 lines).

    Parameters
    ----------
    tool_name : str
        The name of the tool that was executed.
    content : str
        The result/output from the tool.

    Returns
    -------
    Panel
        A Rich Panel displaying the tool result.
    """
    lines = content.split("\n")
    truncated_lines = lines[:5]
    display_content = "\n".join(truncated_lines)

    if len(lines) > 5:
        display_content += "\n..."

    return Panel(
        Text(display_content, style="dim"),
        title=f"[dim]{tool_name} result[/dim]",
        border_style="dim",
        padding=(0, 1),
    )


def render_agent_response(content: str) -> Panel:
    """
    Render an agent response with markdown support.

    Parameters
    ----------
    content : str
        The agent's response content.

    Returns
    -------
    Panel
        A Rich Panel displaying the agent's response.
    """
    markdown_content = Markdown(content)

    return Panel(
        markdown_content,
        title="[cyan]Helix[/cyan]",
        border_style="cyan",
        padding=(1, 2),
    )


def print_prompts_list() -> None:
    """Print the list of available prompts."""
    if not _custom_prompts:
        console.print("[dim]No prompts available.[/dim]")
        console.print(
            "[dim]Add prompts to .helix/prompts/ as .prompt.md files.[/dim]"
        )
        return

    text = Text()
    text.append("Available prompts:\n", style="bold")

    for name, prompt in _custom_prompts.items():
        text.append("\n  ")
        text.append(f"/{name}", style="bold magenta")

        if prompt.description:
            text.append(f" - {prompt.description}", style="dim")

    console.print(Panel(text, border_style="cyan", padding=(1, 2)))


def process_messages(messages: list) -> None:
    """
    Process and display a list of messages from the agent.

    Parameters
    ----------
    messages : list
        List of message objects from the agent.
    """
    for message in messages:
        if isinstance(message, HumanMessage):
            # Skip human messages as they're already displayed
            continue

        if isinstance(message, AIMessage):
            # Check for tool calls
            if hasattr(message, "tool_calls") and message.tool_calls:
                for tool_call in message.tool_calls:
                    panel = render_tool_call(tool_call["name"], tool_call["args"])
                    console.print(panel)

            # Display content if present
            if message.content and str(message.content).strip():
                panel = render_agent_response(str(message.content))
                console.print(panel)

        elif isinstance(message, ToolMessage):
            # Display tool results (first 5 lines)
            tool_name = message.name or "tool"
            panel = render_tool_result(tool_name, str(message.content))
            console.print(panel)


async def invoke_agent(prompt: str) -> None:
    """
    Invoke the agent with streaming and display results in real-time.

    Parameters
    ----------
    prompt : str
        The user's prompt to send to the agent.
    """
    messages = [HumanMessage(content=prompt)]
    config = {"configurable": {"thread_id": THREAD_ID}}

    try:
        async for chunk in graph.astream({"messages": messages}, config):
            # Each chunk contains updates from a node
            # The chunk is a dict with node name as key
            for node_name, node_output in chunk.items():
                if "messages" not in node_output:
                    continue

                new_messages = node_output["messages"]
                for message in new_messages:
                    # Process each new message as it arrives
                    if isinstance(message, AIMessage):
                        # Check for tool calls
                        if hasattr(message, "tool_calls") and message.tool_calls:
                            for tool_call in message.tool_calls:
                                panel = render_tool_call(
                                    tool_call["name"], tool_call["args"]
                                )
                                console.print(panel)

                        # Display content if present
                        if message.content and str(message.content).strip():
                            panel = render_agent_response(str(message.content))
                            console.print(panel)

                    elif isinstance(message, ToolMessage):
                        # Display tool results (first 5 lines)
                        tool_name = message.name or "tool"
                        panel = render_tool_result(tool_name, str(message.content))
                        console.print(panel)

    except Exception as e:
        console.print(
            Panel(
                f"Error: {str(e)}",
                title="[red]Error[/red]",
                border_style="red",
            )
        )


def get_user_prompt() -> str | None:
    """
    Get a prompt from the user.

    Returns
    -------
    str or None
        The user's input, or None if they want to exit.
    """
    console.print()

    try:
        user_input = RichPrompt.ask("[bold green]>[/bold green]", console=console)
    except (KeyboardInterrupt, EOFError):
        return None

    if user_input.strip().lower() == EXIT_COMMAND:
        return None

    return user_input.strip()


def parse_prompt_command(user_input: str) -> tuple[str, str] | None:
    """
    Parse a prompt command from user input.

    Parameters
    ----------
    user_input : str
        The user's input string.

    Returns
    -------
    tuple[str, str] or None
        A tuple of (prompt_name, args) if the input is a valid prompt command,
        None otherwise.
    """
    if not user_input.startswith("/"):
        return None

    parts = user_input[1:].split(maxsplit=1)

    if not parts:
        return None

    prompt_name = parts[0]
    args = parts[1] if len(parts) > 1 else ""

    return (prompt_name, args)


def print_welcome_banner() -> None:
    """Print the welcome banner for Helix."""
    banner = Text()
    banner.append("HELIX", style="bold cyan")
    banner.append("\n", style="")
    banner.append("A coding agent powered by local LLMs", style="dim")
    banner.append("\n\n", style="")
    banner.append("Type ", style="dim")
    banner.append("/clear", style="bold")
    banner.append(" to reset, ", style="dim")
    banner.append("/prompts", style="bold")
    banner.append(" to list prompts, ", style="dim")
    banner.append("/exit", style="bold")
    banner.append(" to quit.", style="dim")

    if _custom_prompts:
        banner.append("\n\n", style="")
        banner.append("Available prompts:", style="dim")

        for name, prompt in _custom_prompts.items():
            banner.append("\n  ", style="")
            banner.append(f"/{name}", style="bold magenta")

            if prompt.description:
                banner.append(f" - {prompt.description}", style="dim")

    console.print(Panel(banner, border_style="cyan", padding=(1, 2)))


async def run_interaction_loop() -> None:
    """Run the main interaction loop for the GUI."""
    global _custom_prompts

    # Load custom prompts at startup
    _custom_prompts = load_prompts()

    print_welcome_banner()

    while True:
        user_prompt = get_user_prompt()

        if user_prompt is None:
            console.print("\n[dim]Goodbye![/dim]")
            break

        if not user_prompt:
            console.print("[dim]Please enter a prompt.[/dim]")
            continue

        # Check for /clear command
        if user_prompt.strip().lower() == CLEAR_COMMAND:
            clear_conversation()
            console.print("[dim]Conversation cleared.[/dim]")
            continue

        # Check for /prompts command
        if user_prompt.strip().lower() == PROMPTS_COMMAND:
            print_prompts_list()
            continue

        # Check if this is a custom prompt command
        parsed = parse_prompt_command(user_prompt)

        if parsed is not None:
            prompt_name, args = parsed

            if prompt_name in _custom_prompts:
                custom_prompt = _custom_prompts[prompt_name]
                rendered_prompt = custom_prompt.render(args)
                await invoke_agent(rendered_prompt)
                continue
            elif prompt_name != "exit":
                console.print(f"[yellow]Unknown prompt: /{prompt_name}[/yellow]")
                continue

        await invoke_agent(user_prompt)


def run_gui() -> None:
    """Run the GUI as a synchronous entry point for the CLI."""
    try:
        asyncio.run(run_interaction_loop())
    except KeyboardInterrupt:
        console.print("\n[dim]Interrupted. Goodbye![/dim]")
