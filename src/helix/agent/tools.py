"""Define tools for the agent to use."""

import platform
import subprocess
from pathlib import Path

from langchain_core.tools import tool


@tool
def read_file(path: str, start_line: int = 1, end_line: int = -1) -> str:
    """Read content from a file.

    Args:
        path: The path to the file to read.
        start_line: The 1-indexed line number to start reading from (default: 1).
        end_line: The 1-indexed line number to stop reading at (inclusive).
                  Use -1 to read until the end of the file (default: -1).

    Returns:
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
    """Write content to a file.

    Creates the file if it doesn't exist, or overwrites it if it does.

    Args:
        path: The path to the file to write.
        content: The content to write to the file.

    Returns:
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
def run_shell_command(command: str) -> str:
    """Execute a shell command and return the output.

    Uses cmd.exe on Windows and bash on other operating systems.

    Args:
        command: The shell command to execute.

    Returns:
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


# List of all available tools
TOOLS = [read_file, write_file, run_shell_command]
