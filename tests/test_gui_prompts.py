"""Tests for the GUI prompt command parsing."""


from helix.gui import parse_prompt_command


class TestParsePromptCommand:
    """Tests for the parse_prompt_command function."""

    def test_parse_valid_command_with_args(self):
        """Test parsing a valid prompt command with arguments."""
        result = parse_prompt_command("/my-prompt hello world")

        assert result is not None
        assert result[0] == "my-prompt"
        assert result[1] == "hello world"

    def test_parse_valid_command_without_args(self):
        """Test parsing a valid prompt command without arguments."""
        result = parse_prompt_command("/my-prompt")

        assert result is not None
        assert result[0] == "my-prompt"
        assert result[1] == ""

    def test_parse_command_with_extra_spaces(self):
        """Test parsing a command with extra spaces in args."""
        result = parse_prompt_command("/test   multiple   spaces")

        assert result is not None
        assert result[0] == "test"
        assert result[1] == "multiple   spaces"

    def test_parse_non_command_returns_none(self):
        """Test that non-command input returns None."""
        result = parse_prompt_command("just a regular message")

        assert result is None

    def test_parse_empty_command_returns_none(self):
        """Test that a lone slash returns None."""
        result = parse_prompt_command("/")

        assert result is None

    def test_parse_exit_command(self):
        """Test parsing the exit command."""
        result = parse_prompt_command("/exit")

        assert result is not None
        assert result[0] == "exit"
        assert result[1] == ""
