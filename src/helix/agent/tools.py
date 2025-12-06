"""Define tools for the agent to use."""

import platform
import subprocess

from langchain_core.tools import tool


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
TOOLS = [run_shell_command]
