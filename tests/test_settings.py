"""Tests for the settings module."""

import json

from helix.settings import (
    Settings,
    Permissions,
    load_settings,
    match_rule,
    check_permission,
    _parse_rule,
    _match_command_pattern,
)


class TestParseRule:
    """Tests for _parse_rule function."""

    def test_simple_tool_name(self):
        """Parse a simple tool name without pattern."""
        tool_name, pattern = _parse_rule("read_file")
        assert tool_name == "read_file"
        assert pattern is None

    def test_tool_with_pattern(self):
        """Parse a tool name with pattern."""
        tool_name, pattern = _parse_rule("run_shell_command(uv:*)")
        assert tool_name == "run_shell_command"
        assert pattern == "uv:*"

    def test_tool_with_complex_pattern(self):
        """Parse a tool name with a complex pattern."""
        tool_name, pattern = _parse_rule("run_shell_command(uv run pytest)")
        assert tool_name == "run_shell_command"
        assert pattern == "uv run pytest"


class TestMatchCommandPattern:
    """Tests for _match_command_pattern function."""

    def test_exact_match(self):
        """Match exact command."""
        assert _match_command_pattern("uv run pytest", "uv run pytest") is True
        assert _match_command_pattern("uv run pytest", "uv run ruff") is False

    def test_wildcard_single_command(self):
        """Match wildcard for single command."""
        assert _match_command_pattern("uv", "uv:*") is True
        assert _match_command_pattern("uv sync", "uv:*") is True
        assert _match_command_pattern("uv run pytest", "uv:*") is True
        assert _match_command_pattern("npm install", "uv:*") is False

    def test_wildcard_subcommand(self):
        """Match wildcard for subcommand."""
        assert _match_command_pattern("uv run", "uv run:*") is True
        assert _match_command_pattern("uv run pytest", "uv run:*") is True
        assert _match_command_pattern("uv run ruff check", "uv run:*") is True
        assert _match_command_pattern("uv sync", "uv run:*") is False

    def test_whitespace_handling(self):
        """Handle whitespace properly."""
        assert _match_command_pattern("  uv run pytest  ", "uv run pytest") is True
        assert _match_command_pattern("uv run pytest", "  uv run pytest  ") is True


class TestMatchRule:
    """Tests for match_rule function."""

    def test_simple_rule_matches_tool(self):
        """Simple rule matches tool name."""
        assert match_rule("read_file", "read_file", {"path": "/foo"}) is True
        assert match_rule("read_file", "write_file", {"path": "/foo"}) is False

    def test_shell_command_with_pattern(self):
        """Shell command rule with pattern."""
        assert match_rule(
            "run_shell_command(uv:*)",
            "run_shell_command",
            {"command": "uv sync"},
        ) is True

        assert match_rule(
            "run_shell_command(uv:*)",
            "run_shell_command",
            {"command": "npm install"},
        ) is False

    def test_shell_command_exact_match(self):
        """Shell command rule with exact command."""
        assert match_rule(
            "run_shell_command(uv run pytest)",
            "run_shell_command",
            {"command": "uv run pytest"},
        ) is True

        assert match_rule(
            "run_shell_command(uv run pytest)",
            "run_shell_command",
            {"command": "uv run ruff"},
        ) is False

    def test_pattern_on_non_shell_tool(self):
        """Pattern on non-shell tool doesn't match."""
        # Patterns only work for run_shell_command
        assert match_rule("read_file(foo)", "read_file", {"path": "/foo"}) is False


class TestCheckPermission:
    """Tests for check_permission function."""

    def test_allow_rule_permits(self):
        """Allow rule permits matching tool."""
        settings = Settings(
            permissions=Permissions(
                allow=["read_file"],
                deny=[],
            )
        )

        result = check_permission(settings, "read_file", {"path": "/foo"})
        assert result is True

    def test_deny_rule_blocks(self):
        """Deny rule blocks matching tool."""
        settings = Settings(
            permissions=Permissions(
                allow=[],
                deny=["write_file"],
            )
        )

        result = check_permission(settings, "write_file", {"path": "/foo"})
        assert result is False

    def test_deny_takes_precedence(self):
        """Deny rules are checked before allow rules."""
        settings = Settings(
            permissions=Permissions(
                allow=["read_file"],
                deny=["read_file"],
            )
        )

        # Deny takes precedence
        result = check_permission(settings, "read_file", {"path": "/foo"})
        assert result is False

    def test_no_matching_rule_returns_none(self):
        """No matching rules returns None for approval."""
        settings = Settings(
            permissions=Permissions(
                allow=["read_file"],
                deny=["write_file"],
            )
        )

        result = check_permission(settings, "insert_text", {"path": "/foo"})
        assert result is None

    def test_shell_command_with_allow_pattern(self):
        """Shell command with allow pattern."""
        settings = Settings(
            permissions=Permissions(
                allow=["run_shell_command(uv:*)"],
                deny=[],
            )
        )

        assert check_permission(
            settings,
            "run_shell_command",
            {"command": "uv sync"},
        ) is True

        assert check_permission(
            settings,
            "run_shell_command",
            {"command": "npm install"},
        ) is None

    def test_shell_command_with_deny_pattern(self):
        """Shell command with deny pattern."""
        settings = Settings(
            permissions=Permissions(
                allow=["run_shell_command(uv:*)"],
                deny=["run_shell_command(uv run pytest:*)"],
            )
        )

        # Allow uv commands in general
        assert check_permission(
            settings,
            "run_shell_command",
            {"command": "uv sync"},
        ) is True

        # But deny uv run pytest specifically
        assert check_permission(
            settings,
            "run_shell_command",
            {"command": "uv run pytest tests/"},
        ) is False


class TestLoadSettings:
    """Tests for load_settings function."""

    def test_load_default_when_no_file(self, tmp_path):
        """Load default settings when file doesn't exist."""
        settings = load_settings(tmp_path)

        assert isinstance(settings, Settings)
        assert settings.permissions.allow == []
        assert settings.permissions.deny == []

    def test_load_default_when_no_directory(self, tmp_path):
        """Load default settings when .helix directory doesn't exist."""
        settings = load_settings(tmp_path / "nonexistent")

        assert isinstance(settings, Settings)
        assert settings.permissions.allow == []
        assert settings.permissions.deny == []

    def test_load_settings_from_file(self, tmp_path):
        """Load settings from JSON file."""
        helix_dir = tmp_path / ".helix"
        helix_dir.mkdir()

        settings_file = helix_dir / "settings.json"
        settings_file.write_text(json.dumps({
            "permissions": {
                "allow": ["read_file", "run_shell_command(uv:*)"],
                "deny": ["write_file"],
            }
        }))

        settings = load_settings(tmp_path)

        assert settings.permissions.allow == ["read_file", "run_shell_command(uv:*)"]
        assert settings.permissions.deny == ["write_file"]

    def test_load_settings_with_invalid_json(self, tmp_path):
        """Load default settings when JSON is invalid."""
        helix_dir = tmp_path / ".helix"
        helix_dir.mkdir()

        settings_file = helix_dir / "settings.json"
        settings_file.write_text("not valid json")

        settings = load_settings(tmp_path)

        assert settings.permissions.allow == []
        assert settings.permissions.deny == []

    def test_load_settings_with_missing_permissions(self, tmp_path):
        """Load settings with missing permissions key."""
        helix_dir = tmp_path / ".helix"
        helix_dir.mkdir()

        settings_file = helix_dir / "settings.json"
        settings_file.write_text(json.dumps({"other_key": "value"}))

        settings = load_settings(tmp_path)

        assert settings.permissions.allow == []
        assert settings.permissions.deny == []
