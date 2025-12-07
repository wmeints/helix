import asyncio
import sys

import click
from rich.console import Console
from rich.panel import Panel
from rich.text import Text

from helix.gui import invoke_agent, run_gui
from helix.ollama import REQUIRED_MODEL, check_ollama_status


def _check_ollama_available() -> bool:
    """
    Check if Ollama is running and the required model is available.

    Displays appropriate error messages if checks fail.

    Returns
    -------
    bool
        True if Ollama is ready, False otherwise.
    """
    console = Console()
    status = check_ollama_status()

    if not status.is_running:
        text = Text()
        text.append("Ollama is not running\n\n", style="bold red")
        text.append("Please start Ollama before using Helix.\n", style="")
        text.append("You can start it with: ", style="dim")
        text.append("ollama serve", style="bold")

        if status.error_message:
            text.append(f"\n\nError: {status.error_message}", style="dim red")

        console.print(Panel(text, title="[red]Error[/red]", border_style="red"))
        return False

    if not status.model_available:
        text = Text()
        text.append(f"Model '{REQUIRED_MODEL}' is not available\n\n", style="bold red")
        text.append("Please pull the model before using Helix.\n", style="")
        text.append("You can pull it with: ", style="dim")
        text.append(f"ollama pull {REQUIRED_MODEL}", style="bold")

        if status.available_models:
            text.append("\n\nAvailable models: ", style="dim")
            text.append(", ".join(status.available_models), style="")

        console.print(Panel(text, title="[red]Error[/red]", border_style="red"))
        return False

    return True


@click.command()
@click.option("-p", "--prompt", default=None, help="The prompt to send to the agent.")
def main(prompt: str | None):
    """Helix - A coding agent powered by local LLMs."""
    # Check Ollama availability before starting
    if not _check_ollama_available():
        sys.exit(1)

    if prompt is not None:
        # Single prompt mode
        asyncio.run(invoke_agent(prompt))
    else:
        # Interactive mode
        run_gui()
