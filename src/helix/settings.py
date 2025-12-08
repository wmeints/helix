"""Settings management for the Helix agent."""

import json
import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


@dataclass
class Permissions:
    """
    Permission rules for tool execution.

    Attributes
    ----------
    allow : list[str]
        List of allow rules. Rules can be:
        - Simple tool name: "read_file", "write_file"
        - Tool with command pattern: "run_shell_command(uv:*)"
    deny : list[str]
        List of deny rules. Same format as allow rules.
    """

    allow: list[str] = field(default_factory=list)
    deny: list[str] = field(default_factory=list)


@dataclass
class Settings:
    """
    Application settings.

    Attributes
    ----------
    permissions : Permissions
        Permission rules for tool execution.
    model : str
        The LLM model to use (default: qwen3-coder).
    context_window_size : int
        The context window size in tokens (default: 128000).
    """

    permissions: Permissions = field(default_factory=Permissions)
    model: str = "qwen3-coder"
    context_window_size: int = 128_000


def _parse_rule(rule: str) -> tuple[str, str | None]:
    """
    Parse a permission rule into tool name and optional pattern.

    Parameters
    ----------
    rule : str
        The rule string, e.g., "read_file" or "run_shell_command(uv:*)".

    Returns
    -------
    tuple[str, str | None]
        A tuple of (tool_name, pattern). Pattern is None for simple rules.
    """
    match = re.match(r"^([a-z_]+)(?:\((.+)\))?$", rule)

    if not match:
        return (rule, None)

    tool_name = match.group(1)
    pattern = match.group(2)

    return (tool_name, pattern)


def _match_command_pattern(command: str, pattern: str) -> bool:
    """
    Match a shell command against a pattern.

    Pattern syntax:
    - "uv" matches exactly "uv"
    - "uv:*" matches "uv" and "uv <anything>"
    - "uv run:*" matches "uv run" and "uv run <anything>"
    - "uv run pytest" matches exactly "uv run pytest"

    Parameters
    ----------
    command : str
        The shell command to check.
    pattern : str
        The pattern to match against.

    Returns
    -------
    bool
        True if the command matches the pattern.
    """
    # Normalize whitespace
    command = command.strip()
    pattern = pattern.strip()

    if pattern.endswith(":*"):
        # Wildcard pattern: match prefix
        prefix = pattern[:-2]
        # Match exactly the prefix, or prefix followed by space and more
        return command == prefix or command.startswith(prefix + " ")

    # Exact match
    return command == pattern


def match_rule(
    rule: str,
    tool_name: str,
    tool_args: dict[str, Any],
) -> bool:
    """
    Check if a tool call matches a permission rule.

    Parameters
    ----------
    rule : str
        The permission rule to check.
    tool_name : str
        The name of the tool being called.
    tool_args : dict[str, Any]
        The arguments passed to the tool.

    Returns
    -------
    bool
        True if the tool call matches the rule.
    """
    rule_tool, rule_pattern = _parse_rule(rule)

    # Tool name must match
    if rule_tool != tool_name:
        return False

    # If no pattern, it's a simple tool match
    if rule_pattern is None:
        return True

    # Pattern matching only applies to run_shell_command
    if tool_name == "run_shell_command":
        command = tool_args.get("command", "")
        return _match_command_pattern(command, rule_pattern)

    # For other tools with patterns, no match (patterns only for shell commands)
    return False


def check_permission(
    settings: Settings,
    tool_name: str,
    tool_args: dict[str, Any],
) -> bool | None:
    """
    Check if a tool call is allowed, denied, or needs user approval.

    Rules are evaluated in order:
    1. If any deny rule matches, return False (denied)
    2. If any allow rule matches, return True (allowed)
    3. If no rules match, return None (requires approval)

    Parameters
    ----------
    settings : Settings
        The application settings.
    tool_name : str
        The name of the tool being called.
    tool_args : dict[str, Any]
        The arguments passed to the tool.

    Returns
    -------
    bool | None
        True if allowed, False if denied, None if requires approval.
    """
    # Check deny rules first
    for rule in settings.permissions.deny:
        if match_rule(rule, tool_name, tool_args):
            return False

    # Check allow rules
    for rule in settings.permissions.allow:
        if match_rule(rule, tool_name, tool_args):
            return True

    # No matching rules - requires approval
    return None


def load_settings(base_path: Path | None = None) -> Settings:
    """
    Load settings from .helix/settings.json.

    If the file or directory doesn't exist, returns default settings.

    Parameters
    ----------
    base_path : Path | None, optional
        The base path to look for .helix/settings.json.
        Defaults to the current working directory.

    Returns
    -------
    Settings
        The loaded settings, or defaults if the file doesn't exist.
    """
    if base_path is None:
        base_path = Path.cwd()

    settings_path = base_path / ".helix" / "settings.json"

    if not settings_path.exists():
        return Settings()

    try:
        with open(settings_path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except (json.JSONDecodeError, OSError):
        return Settings()

    return _parse_settings(data)


def _parse_settings(data: dict[str, Any]) -> Settings:
    """
    Parse settings from a dictionary.

    Parameters
    ----------
    data : dict[str, Any]
        The settings data.

    Returns
    -------
    Settings
        The parsed settings.
    """
    permissions_data = data.get("permissions", {})

    permissions = Permissions(
        allow=permissions_data.get("allow", []),
        deny=permissions_data.get("deny", []),
    )

    model = data.get("model", "qwen3-coder")
    context_window_size = data.get("context_window_size", 128_000)

    return Settings(
        permissions=permissions,
        model=model,
        context_window_size=context_window_size,
    )


# Global settings instance
_settings: Settings | None = None


def get_settings() -> Settings:
    """
    Get the current settings, loading them if necessary.

    Returns
    -------
    Settings
        The current settings.
    """
    global _settings

    if _settings is None:
        _settings = load_settings()

    return _settings


def reload_settings() -> Settings:
    """
    Reload settings from disk.

    Returns
    -------
    Settings
        The reloaded settings.
    """
    global _settings
    _settings = load_settings()
    return _settings


def save_settings(settings: Settings, base_path: Path | None = None) -> None:
    """
    Save settings to .helix/settings.json.

    Parameters
    ----------
    settings : Settings
        The settings to save.
    base_path : Path | None, optional
        The base path for .helix/settings.json.
        Defaults to the current working directory.
    """
    if base_path is None:
        base_path = Path.cwd()

    helix_dir = base_path / ".helix"
    helix_dir.mkdir(parents=True, exist_ok=True)

    settings_path = helix_dir / "settings.json"

    data = {
        "permissions": {
            "allow": settings.permissions.allow,
            "deny": settings.permissions.deny,
        },
        "model": settings.model,
        "context_window_size": settings.context_window_size,
    }

    with open(settings_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
        f.write("\n")


def add_allow_rule(tool_name: str, tool_args: dict[str, Any]) -> str:
    """
    Add an allow rule for a tool and save settings.

    For run_shell_command, creates a pattern rule based on the command prefix.
    For other tools, creates a simple tool name rule.

    Parameters
    ----------
    tool_name : str
        The name of the tool.
    tool_args : dict[str, Any]
        The arguments passed to the tool.

    Returns
    -------
    str
        The rule that was added.
    """
    global _settings

    if _settings is None:
        _settings = load_settings()

    # Create the rule
    if tool_name == "run_shell_command":
        command = tool_args.get("command", "")
        # Use the first word of the command as the prefix with wildcard
        first_word = command.split()[0] if command.split() else command
        rule = f"run_shell_command({first_word}:*)"
    else:
        rule = tool_name

    # Add the rule if not already present
    if rule not in _settings.permissions.allow:
        _settings.permissions.allow.append(rule)
        save_settings(_settings)

    return rule
