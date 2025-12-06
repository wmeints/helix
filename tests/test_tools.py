"""Test the agent tools."""

import json
import tempfile
from pathlib import Path

import pytest
from pydantic import ValidationError

from helix.agent.tools import (
    clear_todos,
    get_todos,
    insert_text,
    read_file,
    read_todos,
    run_shell_command,
    write_file,
    write_todos,
)


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


def test_write_file_creates_new_file():
    """Test that write_file creates a new file with the given content."""
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir) / "new_file.txt"
        content = "Hello, World!"

        result = write_file.invoke({"path": str(temp_path), "content": content})

        assert "Successfully wrote to" in result
        assert temp_path.read_text() == content


def test_write_file_overwrites_existing_file():
    """Test that write_file overwrites an existing file."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("original content")
        temp_path = f.name

    try:
        new_content = "new content"
        result = write_file.invoke({"path": temp_path, "content": new_content})

        assert "Successfully wrote to" in result
        assert Path(temp_path).read_text() == new_content
    finally:
        Path(temp_path).unlink()


def test_write_file_returns_error_for_missing_directory():
    """Test that write_file returns an error when the directory doesn't exist."""
    result = write_file.invoke({
        "path": "/nonexistent/directory/file.txt",
        "content": "test"
    })
    assert "Error: Directory does not exist" in result


def test_insert_text_at_start_of_file():
    """Test that insert_text inserts content at the start of a file."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\nline 2\n")
        temp_path = f.name

    try:
        result = insert_text.invoke({
            "path": temp_path,
            "content": "new first line",
            "line_number": 1
        })

        assert "Successfully inserted" in result
        content = Path(temp_path).read_text()
        lines = content.splitlines()
        assert lines[0] == "new first line"
        assert lines[1] == "line 1"
        assert lines[2] == "line 2"
    finally:
        Path(temp_path).unlink()


def test_insert_text_in_middle_of_file():
    """Test that insert_text inserts content in the middle of a file."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\nline 2\nline 3\n")
        temp_path = f.name

    try:
        result = insert_text.invoke({
            "path": temp_path,
            "content": "inserted line",
            "line_number": 2
        })

        assert "Successfully inserted" in result
        content = Path(temp_path).read_text()
        lines = content.splitlines()
        assert lines[0] == "line 1"
        assert lines[1] == "inserted line"
        assert lines[2] == "line 2"
        assert lines[3] == "line 3"
    finally:
        Path(temp_path).unlink()


def test_insert_text_at_end_of_file():
    """Test that insert_text appends content at the end of a file."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\nline 2\n")
        temp_path = f.name

    try:
        result = insert_text.invoke({
            "path": temp_path,
            "content": "new last line",
            "line_number": 3
        })

        assert "Successfully inserted" in result
        content = Path(temp_path).read_text()
        lines = content.splitlines()
        assert lines[0] == "line 1"
        assert lines[1] == "line 2"
        assert lines[2] == "new last line"
    finally:
        Path(temp_path).unlink()


def test_insert_text_returns_error_for_missing_file():
    """Test that insert_text returns an error when the file doesn't exist."""
    result = insert_text.invoke({
        "path": "/nonexistent/file.txt",
        "content": "test",
        "line_number": 1
    })
    assert "Error: File not found" in result


def test_insert_text_returns_error_for_invalid_line_number():
    """Test that insert_text returns an error for line_number < 1."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\n")
        temp_path = f.name

    try:
        result = insert_text.invoke({
            "path": temp_path,
            "content": "test",
            "line_number": 0
        })
        assert "Error: line_number must be >= 1" in result
    finally:
        Path(temp_path).unlink()


def test_insert_text_returns_error_for_line_number_exceeding_file():
    """Test that insert_text returns an error when line_number exceeds file length + 1."""
    with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", delete=False) as f:
        f.write("line 1\nline 2\n")
        temp_path = f.name

    try:
        result = insert_text.invoke({
            "path": temp_path,
            "content": "test",
            "line_number": 10
        })
        assert "Error: line_number" in result
        assert "exceeds file length" in result
    finally:
        Path(temp_path).unlink()


def test_write_todos_updates_todo_list():
    """Test that write_todos successfully updates the todo list."""
    clear_todos()

    todos = [
        {"description": "First task", "status": "pending"},
        {"description": "Second task", "status": "in_progress"},
        {"description": "Third task", "status": "completed"},
    ]

    result = write_todos.invoke({"todos": todos})

    assert "Successfully updated todo list with 3 item(s)" in result

    stored_todos = get_todos()
    assert len(stored_todos) == 3
    assert stored_todos[0]["description"] == "First task"
    assert stored_todos[0]["status"] == "pending"
    assert stored_todos[1]["status"] == "in_progress"
    assert stored_todos[2]["status"] == "completed"

    clear_todos()


def test_write_todos_replaces_existing_todos():
    """Test that write_todos replaces the existing todo list."""
    clear_todos()

    write_todos.invoke({"todos": [{"description": "Old task", "status": "pending"}]})
    write_todos.invoke({"todos": [{"description": "New task", "status": "completed"}]})

    stored_todos = get_todos()
    assert len(stored_todos) == 1
    assert stored_todos[0]["description"] == "New task"

    clear_todos()


def test_write_todos_raises_error_for_missing_description():
    """Test that write_todos raises a ValidationError when description is missing."""
    clear_todos()

    with pytest.raises(ValidationError):
        write_todos.invoke({"todos": [{"status": "pending"}]})

    clear_todos()


def test_write_todos_raises_error_for_missing_status():
    """Test that write_todos raises a ValidationError when status is missing."""
    clear_todos()

    with pytest.raises(ValidationError):
        write_todos.invoke({"todos": [{"description": "Task without status"}]})

    clear_todos()


def test_write_todos_returns_error_for_invalid_status():
    """Test that write_todos returns an error for invalid status values."""
    clear_todos()

    result = write_todos.invoke({
        "todos": [{"description": "Task", "status": "invalid_status"}]
    })

    assert "Error: Invalid status" in result
    assert "invalid_status" in result

    clear_todos()


def test_write_todos_accepts_empty_list():
    """Test that write_todos accepts an empty list to clear todos."""
    clear_todos()

    write_todos.invoke({"todos": [{"description": "Task", "status": "pending"}]})
    result = write_todos.invoke({"todos": []})

    assert "Successfully updated todo list with 0 item(s)" in result
    assert len(get_todos()) == 0

    clear_todos()


def test_get_todos_returns_copy():
    """Test that get_todos returns a copy of the todo list."""
    clear_todos()

    write_todos.invoke({"todos": [{"description": "Task", "status": "pending"}]})

    todos = get_todos()
    todos.append({"description": "Extra task", "status": "pending"})

    assert len(get_todos()) == 1

    clear_todos()


def test_clear_todos_removes_all_items():
    """Test that clear_todos removes all items from the list."""
    write_todos.invoke({
        "todos": [
            {"description": "Task 1", "status": "pending"},
            {"description": "Task 2", "status": "completed"},
        ]
    })

    clear_todos()

    assert len(get_todos()) == 0


def test_read_todos_returns_json_string():
    """Test that read_todos returns a valid JSON string."""
    clear_todos()

    write_todos.invoke({
        "todos": [
            {"description": "Task 1", "status": "pending"},
            {"description": "Task 2", "status": "in_progress"},
        ]
    })

    result = read_todos.invoke({})
    parsed = json.loads(result)

    assert isinstance(parsed, list)
    assert len(parsed) == 2
    assert parsed[0]["description"] == "Task 1"
    assert parsed[0]["status"] == "pending"
    assert parsed[1]["description"] == "Task 2"
    assert parsed[1]["status"] == "in_progress"

    clear_todos()


def test_read_todos_returns_empty_list_when_no_todos():
    """Test that read_todos returns an empty JSON array when no todos exist."""
    clear_todos()

    result = read_todos.invoke({})
    parsed = json.loads(result)

    assert parsed == []

    clear_todos()
