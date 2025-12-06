import asyncio

import click

from helix.gui import invoke_agent, run_gui


@click.command()
@click.option("-p", "--prompt", default=None, help="The prompt to send to the agent.")
def main(prompt: str | None):
    """Helix - A coding agent powered by local LLMs."""
    if prompt is not None:
        # Single prompt mode
        asyncio.run(invoke_agent(prompt))
    else:
        # Interactive mode
        run_gui()
