# Solution strategy

This section covers the solution strategy for The Helix solution.

## Technology choices

We'll use [Semantic Kernel][SEMANTIC_KERNEL]
for the agent implementation because this is a fully functional
framework. We expect to update to the Microsoft Agent Framework as it becomes
more stable over the next few months.

## Solutions for quality goals

### Q001 - The agent is reasonably safe to use under normal operational conditions

We'll use a basic console interface to ask the user for confirmation before
calling any tools that could potentially break the machine of the user.

To help the user make an informed decision, we'll show the following
information:

- The tool that we want to call on behalf of the user
- The parameter we received for the tool call

This information is presented in a readable format so the user can easily parse
the information and make an informed decision.

### Q002 - The agent is resistant against LLM provider failures

We'll provide a notification to the user when an LLM call fails with the option
to retry the call from the console.

### Q003 - The agent is extensible so developers can improve its functionality

We'll use the Semantic Kernel Plugin model to build the tools for the agent.

#### Developer Extension

Allows the agent to execute shell commands and edit files in the code base.
It includes the following tools:

- **Shell:** Helps the agent run shell commands on the user's computer.
- **TextEditor:** Helps the agent edit files, for example, to insert and replace text.

#### Tasks Extension

Allows the agent to create, start, and complete TODO items so it can execute
a more complex plan. It includes the following tool:

- **Create TODO items:** This allows the agent to create TODO items to establish a plan.
- **Update TODO items:** This allows the agent to mark TODO items as in progress.
- **Complete TODO items:** This allows the agent to complete TODO items.

[SEMANTIC_KERNEL]: https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-architecture?pivots=programming-language-csharp
