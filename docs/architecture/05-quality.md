# Quality

## Code Quality Tools

### Ruff

Helix uses [Ruff](https://github.com/astral-sh/ruff) for linting and code style
enforcement. Ruff is a fast Python linter written in Rust that replaces multiple
tools (flake8, isort, etc.) with a single unified tool.

**Usage:**

```bash
# Check for issues
uv run ruff check

# Auto-fix issues
uv run ruff check --fix
```

**Integration:**

- Run `ruff check` before committing changes
- CI/CD pipelines should fail on linting errors

### Pytest

Helix uses [pytest](https://pytest.org/) with pytest-asyncio for testing.

**Usage:**

```bash
# Run all tests
uv run pytest

# Run a specific test
uv run pytest tests/test_agent.py::test_name -v
```

**Test Organization:**

Tests are located in the `tests/` directory and follow the naming convention
`test_*.py`.

## Quality Scenarios

### Security: Tool Approval

| Scenario          | All tool calls require user approval unless explicitly allowed |
| ----------------- | -------------------------------------------------------------- |
| **Stimulus**      | Agent requests to execute a tool                               |
| **Response**      | Execution interrupts and prompts user for approval             |
| **Measure**       | 100% of tool calls go through approval check                   |

### Usability: Responsive Interface

| Scenario          | Terminal remains responsive during LLM inference               |
| ----------------- | -------------------------------------------------------------- |
| **Stimulus**      | User submits a prompt                                          |
| **Response**      | Streaming output appears progressively                         |
| **Measure**       | First token appears within model's time-to-first-token         |

### Extensibility: Adding New Tools

| Scenario          | Developer adds a new tool to the agent                         |
| ----------------- | -------------------------------------------------------------- |
| **Stimulus**      | Need for new capability (e.g., web search)                     |
| **Response**      | Add function with `@tool` decorator, add to `TOOLS` list       |
| **Measure**       | New tool available with minimal code changes                   |

## Development Workflow

1. **Make changes** to the codebase
2. **Run linter**: `uv run ruff check`
3. **Fix issues**: `uv run ruff check --fix` or manually
4. **Run tests**: `uv run pytest`
5. **Commit** only when lint and tests pass
