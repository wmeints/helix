# Custom Prompts

Helix supports custom prompts that let you create reusable, templated commands
for common tasks. This guide explains how to create and use custom prompts.

## Overview

Custom prompts are Markdown files with YAML front-matter stored in the
`.helix/prompts/` directory. They allow you to:

- Create shortcuts for frequently used instructions
- Build templated prompts with dynamic arguments
- Share common workflows with your team

## Creating a Prompt File

### File Location and Naming

Prompt files must be placed in the `.helix/prompts/` directory in your project
and use the `.prompt.md` extension:

```text
your-project/
├── .helix/
│   └── prompts/
│       ├── review.prompt.md
│       ├── explain.prompt.md
│       └── refactor.prompt.md
└── src/
    └── ...
```

### Prompt File Structure

Each prompt file has two parts:

1. **YAML front-matter** - Metadata about the prompt
2. **Markdown content** - The actual prompt text

```markdown
---
name: review
description: Review code for best practices
---

Please review the following code and suggest improvements focusing on:
- Code readability
- Performance issues
- Potential bugs
- Best practices

{{args}}
```

## Front-matter Fields

The YAML front-matter at the top of the file defines metadata for the prompt:

| Field         | Required | Description                                      |
| ------------- | -------- | ------------------------------------------------ |
| `name`        | Yes      | The command name used to invoke the prompt       |
| `description` | No       | A brief description shown in the prompts list    |

### Name Requirements

The `name` field must:

- Contain only alphanumeric characters and dashes
- Be unique across all prompts in your project

Valid names: `review`, `explain-function`, `fix-bug`, `code-review-v2`

Invalid names: `my prompt`, `review@code`, `explain_function`

### Example Front-matter

```yaml
---
name: test-coverage
description: Analyze test coverage and suggest improvements
---
```

## Using Template Variables

Prompts support Mustache-style template variables. Currently, one variable is
available:

### The `{{args}}` Variable

The `{{args}}` variable is replaced with everything you type after the prompt
command.

**Prompt file (`explain.prompt.md`):**

```markdown
---
name: explain
description: Explain code in detail
---

Please explain the following code in detail, including:
- What it does
- How it works
- Any notable patterns or techniques used

{{args}}
```

**Usage:**

```
/explain src/helix/agent/graph.py
```

**Rendered prompt sent to the agent:**

```
Please explain the following code in detail, including:
- What it does
- How it works
- Any notable patterns or techniques used

src/helix/agent/graph.py
```

### Using Args for Different Purposes

The `{{args}}` variable is flexible. You can use it for:

**File paths:**

```markdown
---
name: review
description: Review a file
---

Review the code in {{args}} and suggest improvements.
```

Usage: `/review src/main.py`

**Inline code or text:**

```markdown
---
name: explain-error
description: Explain an error message
---

Explain this error message and suggest how to fix it:

{{args}}
```

Usage: `/explain-error TypeError: Cannot read property 'map' of undefined`

**Multiple arguments:**

```markdown
---
name: compare
description: Compare two approaches
---

Compare these two approaches and recommend which is better:

{{args}}
```

Usage: `/compare using a class vs using a function for this component`

## Listing Available Prompts

Use the `/prompts` command to see all available prompts:

```
/prompts
```

This displays each prompt's name and description.

## Example Prompts

### Code Review

```markdown
---
name: review
description: Comprehensive code review
---

Please review the code and provide feedback on:

1. **Correctness** - Are there any bugs or logic errors?
2. **Readability** - Is the code easy to understand?
3. **Performance** - Are there any performance concerns?
4. **Security** - Are there any security vulnerabilities?
5. **Best practices** - Does it follow language idioms and conventions?

{{args}}
```

### Bug Fix

```markdown
---
name: fix
description: Fix a bug in the code
---

There's a bug in the code. Please:

1. Identify the root cause
2. Explain why it's happening
3. Provide a fix
4. Suggest how to prevent similar bugs

The issue: {{args}}
```

### Documentation

```markdown
---
name: document
description: Generate documentation for code
---

Please generate comprehensive documentation for the following code:

- Add docstrings following NumPy style
- Include parameter descriptions
- Document return values
- Add usage examples where appropriate

{{args}}
```

### Test Generation

```markdown
---
name: test
description: Generate tests for code
---

Generate comprehensive tests for the following code:

- Cover happy path scenarios
- Include edge cases
- Test error handling
- Use pytest conventions

{{args}}
```

## Next Steps

- [Quickstart](quickstart.md) - Installation and basic usage
- [Settings](settings.md) - Configure permissions and customize behavior
- [Safety](safety.md) - Learn about safety precautions when using the agent
