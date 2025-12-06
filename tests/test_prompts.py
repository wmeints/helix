"""Tests for the custom prompts functionality."""

from pathlib import Path


from helix.prompts import Prompt, load_prompt, load_prompts, _validate_prompt_name


class TestValidatePromptName:
    """Tests for the _validate_prompt_name function."""

    def test_valid_alphanumeric_name(self):
        """Test that alphanumeric names are valid."""
        assert _validate_prompt_name("test123") is True

    def test_valid_name_with_dashes(self):
        """Test that names with dashes are valid."""
        assert _validate_prompt_name("my-prompt") is True

    def test_valid_name_with_numbers_and_dashes(self):
        """Test that names with numbers and dashes are valid."""
        assert _validate_prompt_name("prompt-1-test") is True

    def test_invalid_name_with_spaces(self):
        """Test that names with spaces are invalid."""
        assert _validate_prompt_name("my prompt") is False

    def test_invalid_name_with_underscores(self):
        """Test that names with underscores are invalid."""
        assert _validate_prompt_name("my_prompt") is False

    def test_invalid_name_with_special_characters(self):
        """Test that names with special characters are invalid."""
        assert _validate_prompt_name("prompt@test") is False

    def test_empty_name(self):
        """Test that empty names are invalid."""
        assert _validate_prompt_name("") is False


class TestPromptRender:
    """Tests for the Prompt.render method."""

    def test_render_with_args(self):
        """Test rendering a prompt with args."""
        prompt = Prompt(
            name="test",
            description="A test prompt",
            content="Hello {{args}}!",
        )
        result = prompt.render("world")
        assert result == "Hello world!"

    def test_render_with_empty_args(self):
        """Test rendering a prompt with empty args."""
        prompt = Prompt(
            name="test",
            description=None,
            content="Hello {{args}}!",
        )
        result = prompt.render("")
        assert result == "Hello !"

    def test_render_with_multiple_args_placeholders(self):
        """Test rendering a prompt with multiple args placeholders."""
        prompt = Prompt(
            name="test",
            description=None,
            content="First: {{args}}, Second: {{args}}",
        )
        result = prompt.render("value")
        assert result == "First: value, Second: value"

    def test_render_without_args_placeholder(self):
        """Test rendering a prompt without args placeholder."""
        prompt = Prompt(
            name="test",
            description=None,
            content="This is a static prompt",
        )
        result = prompt.render("ignored")
        assert result == "This is a static prompt"


class TestLoadPrompt:
    """Tests for the load_prompt function."""

    def test_load_valid_prompt(self, tmp_path: Path):
        """Test loading a valid prompt file."""
        prompt_file = tmp_path / "test.prompt.md"
        prompt_file.write_text(
            """---
name: my-prompt
description: A test prompt
---

This is the prompt content with {{args}}."""
        )

        prompt = load_prompt(prompt_file)

        assert prompt is not None
        assert prompt.name == "my-prompt"
        assert prompt.description == "A test prompt"
        assert "{{args}}" in prompt.content

    def test_load_prompt_without_description(self, tmp_path: Path):
        """Test loading a prompt file without description."""
        prompt_file = tmp_path / "test.prompt.md"
        prompt_file.write_text(
            """---
name: simple-prompt
---

Simple content."""
        )

        prompt = load_prompt(prompt_file)

        assert prompt is not None
        assert prompt.name == "simple-prompt"
        assert prompt.description is None
        assert "Simple content" in prompt.content

    def test_load_prompt_without_name_returns_none(self, tmp_path: Path):
        """Test that loading a prompt without name returns None."""
        prompt_file = tmp_path / "test.prompt.md"
        prompt_file.write_text(
            """---
description: Missing name
---

Content here."""
        )

        prompt = load_prompt(prompt_file)

        assert prompt is None

    def test_load_prompt_with_invalid_name_returns_none(self, tmp_path: Path):
        """Test that loading a prompt with invalid name returns None."""
        prompt_file = tmp_path / "test.prompt.md"
        prompt_file.write_text(
            """---
name: invalid_name
---

Content here."""
        )

        prompt = load_prompt(prompt_file)

        assert prompt is None

    def test_load_nonexistent_file_returns_none(self, tmp_path: Path):
        """Test that loading a nonexistent file returns None."""
        prompt_file = tmp_path / "nonexistent.prompt.md"

        prompt = load_prompt(prompt_file)

        assert prompt is None


class TestLoadPrompts:
    """Tests for the load_prompts function."""

    def test_load_prompts_from_directory(self, tmp_path: Path):
        """Test loading all prompts from a directory."""
        prompts_dir = tmp_path / ".helix" / "prompts"
        prompts_dir.mkdir(parents=True)

        (prompts_dir / "first.prompt.md").write_text(
            """---
name: first
description: First prompt
---

First content."""
        )

        (prompts_dir / "second.prompt.md").write_text(
            """---
name: second
---

Second content."""
        )

        prompts = load_prompts(prompts_dir)

        assert len(prompts) == 2
        assert "first" in prompts
        assert "second" in prompts
        assert prompts["first"].description == "First prompt"
        assert prompts["second"].description is None

    def test_load_prompts_skips_invalid_files(self, tmp_path: Path):
        """Test that invalid prompt files are skipped."""
        prompts_dir = tmp_path / ".helix" / "prompts"
        prompts_dir.mkdir(parents=True)

        (prompts_dir / "valid.prompt.md").write_text(
            """---
name: valid
---

Valid content."""
        )

        # Invalid: no name
        (prompts_dir / "invalid.prompt.md").write_text(
            """---
description: No name
---

Invalid content."""
        )

        prompts = load_prompts(prompts_dir)

        assert len(prompts) == 1
        assert "valid" in prompts

    def test_load_prompts_empty_directory(self, tmp_path: Path):
        """Test loading from an empty directory."""
        prompts_dir = tmp_path / ".helix" / "prompts"
        prompts_dir.mkdir(parents=True)

        prompts = load_prompts(prompts_dir)

        assert len(prompts) == 0

    def test_load_prompts_nonexistent_directory(self, tmp_path: Path):
        """Test loading from a nonexistent directory."""
        prompts_dir = tmp_path / ".helix" / "prompts"

        prompts = load_prompts(prompts_dir)

        assert len(prompts) == 0

    def test_load_prompts_default_directory(self, tmp_path: Path, monkeypatch):
        """Test loading from the default .helix/prompts directory."""
        prompts_dir = tmp_path / ".helix" / "prompts"
        prompts_dir.mkdir(parents=True)

        (prompts_dir / "default.prompt.md").write_text(
            """---
name: default
---

Default content."""
        )

        monkeypatch.chdir(tmp_path)

        prompts = load_prompts()

        assert len(prompts) == 1
        assert "default" in prompts

    def test_load_prompts_ignores_non_prompt_files(self, tmp_path: Path):
        """Test that non-.prompt.md files are ignored."""
        prompts_dir = tmp_path / ".helix" / "prompts"
        prompts_dir.mkdir(parents=True)

        (prompts_dir / "valid.prompt.md").write_text(
            """---
name: valid
---

Valid content."""
        )

        # This should be ignored
        (prompts_dir / "readme.md").write_text("# README\n\nThis is a readme.")

        prompts = load_prompts(prompts_dir)

        assert len(prompts) == 1
        assert "valid" in prompts
