"""Custom prompt loading and rendering for Helix."""

import re
from dataclasses import dataclass
from pathlib import Path

import chevron
import frontmatter


@dataclass
class Prompt:
    """A custom prompt loaded from a .prompt.md file.

    Attributes:
        name: The name of the prompt (alphanumeric and dashes only).
        description: Optional description of what the prompt does.
        content: The markdown content of the prompt (may contain mustache templates).
    """

    name: str
    description: str | None
    content: str

    def render(self, args: str) -> str:
        """Render the prompt content with the given arguments.

        Args:
            args: The arguments to pass to the prompt template.

        Returns:
            The rendered prompt content.
        """
        return chevron.render(self.content, {"args": args})


# Pattern for valid prompt names (alphanumeric and dashes only)
VALID_NAME_PATTERN = re.compile(r"^[a-zA-Z0-9-]+$")


def _validate_prompt_name(name: str) -> bool:
    """Validate that a prompt name contains only alphanumeric characters and dashes.

    Args:
        name: The prompt name to validate.

    Returns:
        True if the name is valid, False otherwise.
    """
    return bool(VALID_NAME_PATTERN.match(name))


def load_prompt(file_path: Path) -> Prompt | None:
    """Load a single prompt from a .prompt.md file.

    Args:
        file_path: Path to the .prompt.md file.

    Returns:
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

    description = str(post.metadata.get("description"))
    content = post.content

    return Prompt(name=str(name), description=description, content=content)


def load_prompts(prompts_dir: Path | None = None) -> dict[str, Prompt]:
    """Load all prompts from the .helix/prompts directory.

    Args:
        prompts_dir: Optional path to the prompts directory.
                     Defaults to .helix/prompts in the current working directory.

    Returns:
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
