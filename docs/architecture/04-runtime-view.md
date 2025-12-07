# Runtime View

## Main Interaction Flow

This scenario shows the typical flow when a user provides a prompt that requires
tool usage.

```text
┌──────┐     ┌─────┐     ┌───────────┐     ┌──────────────────┐     ┌───────────┐
│ User │     │ GUI │     │ call_llm  │     │check_tool_approval│     │ call_tool │
└──┬───┘     └──┬──┘     └─────┬─────┘     └────────┬─────────┘     └─────┬─────┘
   │            │              │                    │                     │
   │  prompt    │              │                    │                     │
   ├───────────►│              │                    │                     │
   │            │   invoke     │                    │                     │
   │            ├─────────────►│                    │                     │
   │            │              │                    │                     │
   │            │              │ AI response with   │                     │
   │            │              │ tool calls         │                     │
   │            │◄─────────────┤                    │                     │
   │            │              │                    │                     │
   │            │              │     interrupt      │                     │
   │            │              ├───────────────────►│                     │
   │            │              │                    │                     │
   │            │       tool approval request       │                     │
   │◄───────────┼────────────────────────────────────                     │
   │            │                                   │                     │
   │   approve  │                                   │                     │
   ├───────────►│                                   │                     │
   │            │              │    resume          │                     │
   │            ├──────────────┼───────────────────►│                     │
   │            │              │                    │                     │
   │            │              │                    │    execute tool     │
   │            │              │                    ├────────────────────►│
   │            │              │                    │                     │
   │            │              │                    │    tool result      │
   │            │              │◄───────────────────┼─────────────────────┤
   │            │              │                    │                     │
   │            │              │ loop back          │                     │
   │            │              ├────────────────────┘                     │
   │            │              │                    │                     │
   │            │   response   │                    │                     │
   │◄───────────┼──────────────┤                    │                     │
   │            │              │                    │                     │
```

### Flow Description

1. **User enters prompt**: The user types a request in the terminal
2. **GUI invokes graph**: The prompt is wrapped in a HumanMessage and sent to
   the LangGraph agent
3. **LLM generates response**: The model processes the prompt and may request
   tool calls
4. **Tool approval check**: If tool calls are present, execution interrupts
5. **User approval**: The GUI presents the tool call details and asks for
   approval (yes/no/always)
6. **Tool execution**: If approved, the tool runs and returns results
7. **Loop continues**: Results are sent back to the LLM for further processing
8. **Response displayed**: When no more tool calls are needed, the final
   response is shown

## Tool Approval Interrupt

This scenario details the interrupt mechanism for tool approval.

```text
┌──────┐     ┌─────────┐     ┌──────────────────┐     ┌──────────┐
│ User │     │   GUI   │     │check_tool_approval│     │ Settings │
└──┬───┘     └────┬────┘     └────────┬─────────┘     └────┬─────┘
   │              │                   │                    │
   │              │   interrupt with  │                    │
   │              │   tool details    │                    │
   │              │◄──────────────────┤                    │
   │              │                   │                    │
   │              │   check settings  │                    │
   │              ├────────────────────────────────────────►
   │              │                   │                    │
   │              │   permission result (allow/deny/none)  │
   │              │◄────────────────────────────────────────
   │              │                   │                    │
   │              │                   │                    │
  ╔══════════════════════════════════════════════════════════════╗
  ║ ALT: Auto-allowed by settings                                ║
  ╠══════════════════════════════════════════════════════════════╣
  ║ │              │                   │                    │    ║
  ║ │              │   resume(approved)│                    │    ║
  ║ │              ├──────────────────►│                    │    ║
  ║ │              │                   │                    │    ║
  ╠══════════════════════════════════════════════════════════════╣
  ║ ALT: Auto-denied by settings                                 ║
  ╠══════════════════════════════════════════════════════════════╣
  ║ │              │                   │                    │    ║
  ║ │              │   resume(declined)│                    │    ║
  ║ │              ├──────────────────►│                    │    ║
  ║ │              │                   │                    │    ║
  ╠══════════════════════════════════════════════════════════════╣
  ║ ALT: Requires user approval                                  ║
  ╠══════════════════════════════════════════════════════════════╣
  ║ │  show tool   │                   │                    │    ║
  ║ │◄─────────────┤                   │                    │    ║
  ║ │              │                   │                    │    ║
  ║ │  choice      │                   │                    │    ║
  ║ ├─────────────►│                   │                    │    ║
  ║ │              │                   │                    │    ║
  ║ │              │  (if "always")    │                    │    ║
  ║ │              │  save allow rule  │                    │    ║
  ║ │              ├────────────────────────────────────────►    ║
  ║ │              │                   │                    │    ║
  ║ │              │  resume(result)   │                    │    ║
  ║ │              ├──────────────────►│                    │    ║
  ╚══════════════════════════════════════════════════════════════╝
   │              │                   │                    │
```

### Approval Options

| Option   | Behavior                                                    |
| -------- | ----------------------------------------------------------- |
| yes (y)  | Allow this specific tool call once                          |
| no (n)   | Decline this tool call; agent receives a decline message    |
| always (a)| Allow and save a permission rule for future auto-approval  |

### Permission Rules

Permission rules are stored in `.helix/settings.json`:

```json
{
  "permissions": {
    "allow": [
      "read_file",
      "run_shell_command(uv:*)"
    ],
    "deny": []
  }
}
```

Rules are evaluated in order:

1. Deny rules are checked first (if match, tool is blocked)
2. Allow rules are checked next (if match, tool is auto-approved)
3. If no rules match, user is prompted for approval
