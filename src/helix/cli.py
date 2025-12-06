import click


@click.command()
@click.option("-p", "--prompt", default=None, help="The prompt to send to the agent.")
def main(prompt: str | None):
    """Helix - A coding agent powered by local LLMs."""
    if prompt is None:
        prompt = click.prompt("Enter your prompt")

    click.echo(f"Prompt: {prompt}")
