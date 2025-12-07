"""Rich-based terminal GUI for the Helix coding agent."""

import asyncio
from typing import Any

from langchain_core.messages import AIMessage, HumanMessage, ToolMessage
from langgraph.types import Command
from rich.console import Console
from rich.markdown import Markdown
from rich.panel import Panel
from rich.prompt import Prompt as RichPrompt
from rich.text import Text

from helix.agent.graph import THREAD_ID, clear_conversation, graph
from helix.prompts import Prompt, load_prompts
from helix.settings import add_allow_rule, check_permission, get_settings

# Global console instance
console = Console()

# Built-in commands
EXIT_COMMAND = "/exit"
CLEAR_COMMAND = "/clear"
PROMPTS_COMMAND = "/prompts"

# Loaded custom prompts
_custom_prompts: dict[str, Prompt] = {}


def _render_read_todos_call(tool_args: dict[str, Any]) -> Panel:
    """
    Render the read_todos tool call.

    Parameters
    ----------
    tool_args : dict[str, Any]
        Dictionary of argument names to values.

    Returns
    -------
    Panel
        A Rich Panel with friendly messaging.
    """
    text = Text()
    text.append("Looking up todo items...", style="cyan")
    return Panel(
        text,
        border_style="cyan",
        padding=(0, 1),
    )


def _render_write_todos_call(tool_args: dict[str, Any]) -> Panel:
    """
    Render the write_todos tool call.

    Parameters
    ----------
    tool_args : dict[str, Any]
        Dictionary of argument names to values.

    Returns
    -------
    Panel
        A Rich Panel with friendly messaging.
    """
    todos = tool_args.get("todos", [])

    # Check if all todos are completed
    all_completed = (
        all(todo.get("status") == "completed" for todo in todos) if todos else False
    )

    if all_completed and todos:
        text = Text()
        text.append("Agent has finished all tasks", style="green bold")
        return Panel(
            text,
            border_style="green",
            padding=(0, 1),
        )

    # Check if there's a todo marked as in_progress
    in_progress_todo = None
    for todo in todos:
        if todo.get("status") == "in_progress":
            in_progress_todo = todo
            break

    if in_progress_todo:
        text = Text()
        text.append("Working on: ", style="yellow")
        text.append(in_progress_todo.get("description", "Unknown task"), style="bold")
        return Panel(
            text,
            border_style="yellow",
            padding=(0, 1),
        )

    # Fallback: show how many todos were updated
    text = Text()
    text.append(f"Updated todo list with {len(todos)} item(s)", style="cyan")
    return Panel(
        text,
        border_style="cyan",
        padding=(0, 1),
    )


def _render_read_file_call(tool_args: dict[str, Any]) -> Panel:
    """
    Render the read_file tool call.

    Parameters
    ----------
    tool_args : dict[str, Any]
        Dictionary of argument names to values.

    Returns
    -------
    Panel
        A Rich Panel with friendly messaging.
    """
    path = tool_args.get("path", "unknown")
    start_line = tool_args.get("start_line", 1)
    end_line = tool_args.get("end_line", -1)

    text = Text()
    text.append("Reading ", style="cyan")
    text.append(path, style="bold")

    if end_line == -1:
        if start_line == 1:
            text.append(" (entire file)", style="dim")
        else:
            text.append(f" (from line {start_line} to end)", style="dim")
    else:
        line_count = end_line - start_line + 1
        text.append(
            f" (lines {start_line}-{end_line}, {line_count} lines)", style="dim"
        )

    return Panel(
        text,
        border_style="cyan",
        padding=(0, 1),
    )


def _render_write_file_call(tool_args: dict[str, Any]) -> Panel:
    """
    Render the write_file tool call.

    Parameters
    ----------
    tool_args : dict[str, Any]
        Dictionary of argument names to values.

    Returns
    -------
    Panel
        A Rich Panel with friendly messaging.
    """
    path = tool_args.get("path", "unknown")
    content = tool_args.get("content", "")
    line_count = len(content.split("\n"))

    text = Text()
    text.append("Writing ", style="green")
    text.append(f"{line_count} line(s)", style="bold")
    text.append(" to ", style="green")
    text.append(path, style="bold")

    return Panel(
        text,
        border_style="green",
        padding=(0, 1),
    )


def _render_insert_text_call(tool_args: dict[str, Any]) -> Panel:
    """
    Render the insert_text tool call.

    Parameters
    ----------
    tool_args : dict[str, Any]
        Dictionary of argument names to values.

    Returns
    -------
    Panel
        A Rich Panel with friendly messaging.
    """
    path = tool_args.get("path", "unknown")
    line_number = tool_args.get("line_number", 0)
    content = tool_args.get("content", "")
    line_count = len(content.split("\n"))

    text = Text()
    text.append("Inserting ", style="yellow")
    text.append(f"{line_count} line(s)", style="bold")
    text.append(" at line ", style="yellow")
    text.append(str(line_number), style="bold")
    text.append(" in ", style="yellow")
    text.append(path, style="bold")

    return Panel(
        text,
        border_style="yellow",
        padding=(0, 1),
    )


def _render_run_shell_command_call(tool_args: dict[str, Any]) -> Panel:
    """
    Render the run_shell_command tool call.

    Parameters
    ----------
    tool_args : dict[str, Any]
        Dictionary of argument names to values.

    Returns
    -------
    Panel
        A Rich Panel with friendly messaging.
    """
    command = tool_args.get("command", "unknown")

    text = Text()
    text.append("Executing: ", style="magenta")
    text.append(command, style="bold")

    return Panel(
        text,
        border_style="magenta",
        padding=(0, 1),
    )


# Dispatch table mapping tool names to their render functions
_TOOL_RENDERERS: dict[str, callable] = {
    "read_todos": _render_read_todos_call,
    "write_todos": _render_write_todos_call,
    "read_file": _render_read_file_call,
    "write_file": _render_write_file_call,
    "insert_text": _render_insert_text_call,
    "run_shell_command": _render_run_shell_command_call,
}


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
    renderer = _TOOL_RENDERERS.get(tool_name)

    if renderer is not None:
        return renderer(tool_args)

    err_message = f"No rendering found for tool {tool_name}"
    raise ValueError(err_message)


def get_tool_result_max_lines(tool_name: str) -> int:
    """
    Get the maximum number of lines to display for a tool result.

    Parameters
    ----------
    tool_name : str
        The name of the tool.

    Returns
    -------
    int
        The maximum number of lines to display.
    """
    if tool_name == "run_shell_command":
        return 10
    return 5


def render_tool_result(tool_name: str, content: str) -> Panel:
    """
    Render the result of a tool execution.

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
    max_lines = get_tool_result_max_lines(tool_name)
    lines = content.split("\n")
    truncated_lines = lines[:max_lines]
    display_content = "\n".join(truncated_lines)

    if len(lines) > max_lines:
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
        console.print("[dim]Add prompts to .helix/prompts/ as .prompt.md files.[/dim]")
        return

    text = Text()
    text.append("Available prompts:\n", style="bold")

    for name, prompt in _custom_prompts.items():
        text.append("\n  ")
        text.append(f"/{name}", style="bold magenta")

        if prompt.description:
            text.append(f" - {prompt.description}", style="dim")

    console.print(Panel(text, border_style="cyan", padding=(1, 2)))


def render_tool_auto_approved(tool_name: str, tool_args: dict[str, Any]) -> Panel:
    """
    Render a message for a tool that was auto-approved by settings.

    Parameters
    ----------
    tool_name : str
        The name of the tool.
    tool_args : dict[str, Any]
        The arguments being passed to the tool.

    Returns
    -------
    Panel
        A Rich Panel displaying the auto-approval message.
    """
    text = Text()
    text.append("Auto-approved: ", style="green")
    text.append(tool_name, style="bold magenta")

    # Show command for shell commands
    if tool_name == "run_shell_command" and "command" in tool_args:
        text.append(" (", style="dim")
        command = tool_args["command"]
        if len(command) > 50:
            command = command[:50] + "..."
        text.append(command, style="cyan")
        text.append(")", style="dim")

    return Panel(
        text,
        border_style="green",
        padding=(0, 1),
    )


def render_tool_denied(tool_name: str, tool_args: dict[str, Any]) -> Panel:
    """
    Render a message for a tool that was denied by settings.

    Parameters
    ----------
    tool_name : str
        The name of the tool.
    tool_args : dict[str, Any]
        The arguments being passed to the tool.

    Returns
    -------
    Panel
        A Rich Panel displaying the denial message.
    """
    text = Text()
    text.append("Denied by settings: ", style="red")
    text.append(tool_name, style="bold magenta")

    # Show command for shell commands
    if tool_name == "run_shell_command" and "command" in tool_args:
        text.append(" (", style="dim")
        command = tool_args["command"]
        if len(command) > 50:
            command = command[:50] + "..."
        text.append(command, style="cyan")
        text.append(")", style="dim")

    return Panel(
        text,
        border_style="red",
        padding=(0, 1),
    )


def check_tool_permission(
    tool_name: str,
    tool_args: dict[str, Any],
) -> dict[str, Any]:
    """
    Check tool permission and return approval response.

    This function checks the settings for permission rules and returns
    the appropriate approval response. If allowed or denied by settings,
    it displays visual feedback. If no rule matches, it prompts the user.

    Parameters
    ----------
    tool_name : str
        The name of the tool to check.
    tool_args : dict[str, Any]
        The arguments being passed to the tool.

    Returns
    -------
    dict[str, Any]
        A dict with 'approved' (bool) and optionally 'reason' (str).
    """
    settings = get_settings()
    permission = check_permission(settings, tool_name, tool_args)

    if permission is True:
        # Allowed by settings
        console.print(render_tool_auto_approved(tool_name, tool_args))
        return {"approved": True}

    if permission is False:
        # Denied by settings
        console.print(render_tool_denied(tool_name, tool_args))
        return {"approved": False, "reason": "denied by settings"}

    # No matching rule - prompt user
    return prompt_tool_approval(tool_name, tool_args)


def prompt_tool_approval(tool_name: str, tool_args: dict[str, Any]) -> dict[str, Any]:
    """
    Prompt the user to approve or decline a tool call.

    Options:
    - yes: Allow this tool call once
    - no: Deny this tool call
    - always: Allow and create a permission rule for future calls

    Parameters
    ----------
    tool_name : str
        The name of the tool to approve.
    tool_args : dict[str, Any]
        The arguments being passed to the tool.

    Returns
    -------
    dict[str, Any]
        A dict with 'approved' (bool) and optionally 'reason' (str).
    """
    text = Text()
    text.append("The agent wants to use: ", style="yellow")
    text.append(tool_name, style="bold magenta")
    text.append("\n\n", style="")

    # Format arguments for display
    for key, value in tool_args.items():
        text.append(f"  {key}: ", style="dim")
        value_str = str(value)
        if len(value_str) > 100:
            value_str = value_str[:100] + "..."
        text.append(f"{value_str}\n", style="")

    console.print(
        Panel(
            text,
            title="[yellow]Tool Approval[/yellow]",
            border_style="yellow",
            padding=(1, 2),
        )
    )

    choice = RichPrompt.ask(
        "Allow this tool? [bold](y)es[/bold] / [bold](n)o[/bold] / [bold](a)lways[/bold]",
        console=console,
        choices=["y", "n", "a", "yes", "no", "always"],
        default="n",
    )

    if choice in ("y", "yes"):
        return {"approved": True}
    elif choice in ("a", "always"):
        rule = add_allow_rule(tool_name, tool_args)
        console.print(f"[green]Added allow rule:[/green] {rule}")
        return {"approved": True}
    else:
        return {"approved": False, "reason": "declined by user"}


async def invoke_agent(prompt: str) -> None:
    """
    Invoke the agent with streaming and display results in real-time.

    Handles interrupts for shell command approval.

    Parameters
    ----------
    prompt : str
        The user's prompt to send to the agent.
    """
    messages = [HumanMessage(content=prompt)]
    config = {"configurable": {"thread_id": THREAD_ID}}

    try:
        input_data: dict[str, Any] | None = {"messages": messages}

        while True:
            async for chunk in graph.astream(input_data, config):
                # Each chunk contains updates from a node
                # The chunk is a dict with node name as key
                for node_name, node_output in chunk.items():
                    if node_output is None or "messages" not in node_output:
                        continue

                    new_messages = node_output["messages"]
                    for message in new_messages:
                        # Process each new message as it arrives
                        if isinstance(message, AIMessage):
                            # Display content first if present
                            if message.content and str(message.content).strip():
                                panel = render_agent_response(str(message.content))
                                console.print(panel)

                            # Then check for tool calls
                            if hasattr(message, "tool_calls") and message.tool_calls:
                                for tool_call in message.tool_calls:
                                    panel = render_tool_call(
                                        tool_call["name"], tool_call["args"]
                                    )
                                    console.print(panel)
                        elif isinstance(message, ToolMessage):
                            # Display tool results (first 5 lines), unless suppressed
                            tool_name = message.name or "tool"
                            panel = render_tool_result(tool_name, str(message.content))
                            console.print(panel)

            # Check for interrupts after streaming completes
            state = graph.get_state(config)

            if state.tasks and any(
                task.interrupts for task in state.tasks if hasattr(task, "interrupts")
            ):
                # Handle interrupt - get the interrupt data
                for task in state.tasks:
                    if hasattr(task, "interrupts") and task.interrupts:
                        for interrupt_data in task.interrupts:
                            if (
                                hasattr(interrupt_data, "value")
                                and isinstance(interrupt_data.value, dict)
                                and interrupt_data.value.get("type") == "tool_approval"
                            ):
                                tool_name = interrupt_data.value.get("tool_name", "")
                                tool_args = interrupt_data.value.get("tool_args", {})

                                # Check permissions and prompt user if needed
                                approval = check_tool_permission(tool_name, tool_args)

                                # Resume with the approval result
                                input_data = Command(resume=approval)
                                break
                        else:
                            continue
                        break
                else:
                    # No tool approval interrupt found, exit loop
                    break
            else:
                # No interrupts, exit loop
                break

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
        user_input = RichPrompt.ask("[bold green]Prompt[/bold green]", console=console)
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
