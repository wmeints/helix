"""Test the agent tools."""

import tempfile
from pathlib import Path

from helix.agent.tools import read_file, run_shell_command


def test_run_shell_command_executes_echo():
    """Test that run_shell_command can execute a simple echo command."""
    result = run_shell_command.invoke("echo hello")
    assert "hello" in result


def test_read_file_reads_entire_file():
    """Test that read_file reads an entire file when no line range is specified."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\nline 2\nline 3\n")
        temp_path = f.name

    try:
        result = read_file.invoke({"path": temp_path})
        assert "1: line 1" in result
        assert "2: line 2" in result
        assert "3: line 3" in result
    finally:
        Path(temp_path).unlink()


def test_read_file_reads_line_range():
    """Test that read_file reads a specific range of lines."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\nline 2\nline 3\nline 4\nline 5\n")
        temp_path = f.name

    try:
        result = read_file.invoke({"path": temp_path, "start_line": 2, "end_line": 4})
        assert "1: line 1" not in result
        assert "2: line 2" in result
        assert "3: line 3" in result
        assert "4: line 4" in result
        assert "5: line 5" not in result
    finally:
        Path(temp_path).unlink()


def test_read_file_reads_from_start_to_end():
    """Test that read_file reads from start_line to end when end_line is -1."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\nline 2\nline 3\n")
        temp_path = f.name

    try:
        result = read_file.invoke({"path": temp_path, "start_line": 2, "end_line": -1})
        assert "1: line 1" not in result
        assert "2: line 2" in result
        assert "3: line 3" in result
    finally:
        Path(temp_path).unlink()


def test_read_file_returns_error_for_missing_file():
    """Test that read_file returns an error for a missing file."""
    result = read_file.invoke({"path": "/nonexistent/path/file.txt"})
    assert "Error: File not found" in result


def test_read_file_returns_error_for_invalid_start_line():
    """Test that read_file returns an error for start_line < 1."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\n")
        temp_path = f.name

    try:
        result = read_file.invoke({"path": temp_path, "start_line": 0})
        assert "Error: start_line must be >= 1" in result
    finally:
        Path(temp_path).unlink()
