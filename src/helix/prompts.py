"""Custom prompt loading and rendering for Helix."""

import re
from dataclasses import dataclass
from pathlib import Path

import chevron
import frontmatter


@dataclass
class Prompt:
    """
    A custom prompt loaded from a .prompt.md file.

    Attributes
    ----------
    name : str
        The name of the prompt (alphanumeric and dashes only).
    description : str or None
        Optional description of what the prompt does.
    content : str
        The markdown content of the prompt (may contain mustache templates).
    """

    name: str
    description: str | None
    content: str

    def render(self, args: str) -> str:
        """
        Render the prompt content with the given arguments.

        Parameters
        ----------
        args : str
            The arguments to pass to the prompt template.

        Returns
        -------
        str
            The rendered prompt content.
        """
        return chevron.render(self.content, {"args": args})


# Pattern for valid prompt names (alphanumeric and dashes only)
VALID_NAME_PATTERN = re.compile(r"^[a-zA-Z0-9-]+$")


def _validate_prompt_name(name: str) -> bool:
    """
    Validate that a prompt name contains only alphanumeric characters and dashes.

    Parameters
    ----------
    name : str
        The prompt name to validate.

    Returns
    -------
    bool
        True if the name is valid, False otherwise.
    """
    return bool(VALID_NAME_PATTERN.match(name))


def load_prompt(file_path: Path) -> Prompt | None:
    """
    Load a single prompt from a .prompt.md file.

    Parameters
    ----------
    file_path : Path
        Path to the .prompt.md file.

    Returns
    -------
    Prompt or None
        A Prompt object if the file is valid, None otherwise.
    """
    try:
        post = frontmatter.load(str(file_path))
    except Exception:
        return None

    name = post.metadata.get("name")

    if not name:
        return None

    if not _validate_prompt_name(str(name)):
        return None

    raw_description = post.metadata.get("description")
    description = str(raw_description) if raw_description is not None else None
    content = post.content

    return Prompt(name=str(name), description=description, content=content)


def load_prompts(prompts_dir: Path | None = None) -> dict[str, Prompt]:
    """
    Load all prompts from the .helix/prompts directory.

    Parameters
    ----------
    prompts_dir : Path or None, optional
        Path to the prompts directory.
        Defaults to .helix/prompts in the current working directory.

    Returns
    -------
    dict[str, Prompt]
        A dictionary mapping prompt names to Prompt objects.
    """
    if prompts_dir is None:
        prompts_dir = Path.cwd() / ".helix" / "prompts"

    prompts: dict[str, Prompt] = {}

    if not prompts_dir.exists() or not prompts_dir.is_dir():
        return prompts

    for file_path in prompts_dir.glob("*.prompt.md"):
        prompt = load_prompt(file_path)

        if prompt is not None:
            prompts[prompt.name] = prompt

    return prompts
