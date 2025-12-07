"""Test the agent's happy flow."""

from helix.agent.graph import _load_custom_instructions


def test_load_custom_instructions_returns_none_when_file_missing(tmp_path, monkeypatch):
    """Test that _load_custom_instructions returns None when AGENTS.md doesn't exist."""
    monkeypatch.chdir(tmp_path)
    result = _load_custom_instructions()
    assert result is None


def test_load_custom_instructions_returns_content_when_file_exists(tmp_path, monkeypatch):
    """Test that _load_custom_instructions returns file contents when AGENTS.md exists."""
    agents_md = tmp_path / "AGENTS.md"
    agents_md.write_text("Custom instructions for the agent")
    monkeypatch.chdir(tmp_path)

    result = _load_custom_instructions()
    assert result == "Custom instructions for the agent"

