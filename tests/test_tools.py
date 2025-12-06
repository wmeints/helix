"""Test the agent tools."""

from helix.agent.tools import run_shell_command


def test_run_shell_command_executes_echo():
    """Test that run_shell_command can execute a simple echo command."""
    result = run_shell_command.invoke("echo hello")
    assert "hello" in result
