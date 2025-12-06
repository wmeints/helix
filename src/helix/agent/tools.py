"""Define tools for the agent to use."""

import json
import platform
import subprocess
from enum import Enum
from pathlib import Path
from typing import TypedDict

from langchain_core.tools import tool


class TodoStatus(str, Enum):
    """Status options for a todo item."""

    PENDING = "pending"
    IN_PROGRESS = "in_progress"
    COMPLETED = "completed"


class TodoItem(TypedDict):
    """
    A todo item with description and status.

    Attributes
    ----------
    description : str
        The description of the todo item.
    status : str
        The status of the todo item (pending, in_progress, completed).
    """

    description: str
    status: str


# In-memory storage for todo items
_todos: list[TodoItem] = []


@tool
def read_file(path: str, start_line: int = 1, end_line: int = -1) -> str:
    """
    Read content from a file.

    Parameters
    ----------
    path : str
        The path to the file to read.
    start_line : int, optional
        The 1-indexed line number to start reading from (default: 1).
    end_line : int, optional
        The 1-indexed line number to stop reading at (inclusive).
        Use -1 to read until the end of the file (default: -1).

    Returns
    -------
    str
        The file content with line numbers, or an error message.
    """
    try:
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()

        if start_line < 1:
            return "Error: start_line must be >= 1."

        start_idx = start_line - 1

        if start_idx >= len(lines):
            return f"Error: start_line {start_line} exceeds file length ({len(lines)} lines)."

        if end_line == -1:
            end_idx = len(lines)
        else:
            if end_line < start_line:
                return "Error: end_line must be >= start_line."
            end_idx = min(end_line, len(lines))

        selected_lines = lines[start_idx:end_idx]

        result = []
        for i, line in enumerate(selected_lines, start=start_line):
            result.append(f"{i}: {line.rstrip()}")

        return "\n".join(result)
    except FileNotFoundError:
        return f"Error: File not found: {path}"
    except PermissionError:
        return f"Error: Permission denied: {path}"
    except Exception as e:
        return f"Error reading file: {str(e)}"


@tool
def write_file(path: str, content: str) -> str:
    """
    Write content to a file.

    Creates the file if it doesn't exist, or overwrites it if it does.

    Parameters
    ----------
    path : str
        The path to the file to write.
    content : str
        The content to write to the file.

    Returns
    -------
    str
        A success message, or an error message if the operation failed.
    """
    file_path = Path(path)
    parent_dir = file_path.parent

    if not parent_dir.exists():
        return f"Error: Directory does not exist: {parent_dir}"

    try:
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        return f"Successfully wrote to {path}"
    except PermissionError:
        return f"Error: Permission denied: {path}"
    except Exception as e:
        return f"Error writing file: {str(e)}"


@tool
def insert_text(path: str, content: str, line_number: int) -> str:
    """
    Insert text at a specific line in a file.

    Parameters
    ----------
    path : str
        The path to the file to modify.
    content : str
        The content to insert.
    line_number : int
        The 1-indexed line number to insert the content at.
        Use 1 to insert at the start of the file.
        Use a value equal to the number of lines + 1 to append at the end.

    Returns
    -------
    str
        A success message, or an error message if the operation failed.
    """
    file_path = Path(path)

    if not file_path.exists():
        return f"Error: File not found: {path}"

    try:
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()

        if line_number < 1:
            return "Error: line_number must be >= 1."

        # Allow inserting at line_number == len(lines) + 1 (append to end)
        if line_number > len(lines) + 1:
            return f"Error: line_number {line_number} exceeds file length + 1 ({len(lines) + 1})."

        # Ensure content ends with newline for proper insertion
        if content and not content.endswith("\n"):
            content += "\n"

        # Insert at the specified position (0-indexed)
        insert_idx = line_number - 1
        lines.insert(insert_idx, content)

        with open(path, "w", encoding="utf-8") as f:
            f.writelines(lines)

        return f"Successfully inserted text at line {line_number} in {path}"
    except PermissionError:
        return f"Error: Permission denied: {path}"
    except Exception as e:
        return f"Error inserting text: {str(e)}"


@tool
def run_shell_command(command: str) -> str:
    """
    Execute a shell command and return the output.

    Uses cmd.exe on Windows and bash on other operating systems.

    Parameters
    ----------
    command : str
        The shell command to execute.

    Returns
    -------
    str
        The stdout and stderr output from the command, or an error message.
    """
    try:
        if platform.system() == "Windows":
            result = subprocess.run(
                ["cmd.exe", "/c", command],
                capture_output=True,
                text=True,
                timeout=60,
            )
        else:
            result = subprocess.run(
                ["bash", "-c", command],
                capture_output=True,
                text=True,
                timeout=60,
            )

        output = result.stdout
        if result.stderr:
            output += f"\n{result.stderr}" if output else result.stderr

        if not output.strip():
            return f"Command executed successfully (exit code {result.returncode})."

        return output.strip()
    except subprocess.TimeoutExpired:
        return "Error: Command timed out after 60 seconds."
    except Exception as e:
        return f"Error executing command: {str(e)}"


@tool
def write_todos(todos: list[TodoItem]) -> str:
    """
    Write todo items to the agent's in-memory todo list.

    This replaces the current todo list with the provided items.

    Parameters
    ----------
    todos : list[TodoItem]
        A list of todo items. Each item must have:
        - description: str - The description of the todo item.
        - status: str - The status (pending, in_progress, completed).

    Returns
    -------
    str
        A success message confirming the todos were updated.
    """
    global _todos

    valid_statuses = {s.value for s in TodoStatus}

    for todo in todos:
        if todo["status"] not in valid_statuses:
            return (
                f"Error: Invalid status '{todo['status']}'. "
                f"Must be one of: {', '.join(valid_statuses)}."
            )

    _todos = list(todos)
    return f"Successfully updated todo list with {len(_todos)} item(s)."


@tool
def read_todos() -> str:
    """
    Read the current list of todo items.

    Returns
    -------
    str
        A JSON string containing the list of todo items.
        Each item has 'description' and 'status' fields.
    """
    return json.dumps(_todos, indent=2)


def get_todos() -> list[TodoItem]:
    """
    Get the current list of todo items.

    Returns
    -------
    list[TodoItem]
        The current list of todo items.
    """
    return _todos.copy()


def clear_todos() -> None:
    """Clear all todo items from memory."""
    global _todos
    _todos = []


# List of all available tools
TOOLS = [read_file, write_file, insert_text, run_shell_command, write_todos, read_todos]
