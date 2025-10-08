# Introduction and goals

SpecForge is a console-based coding agent that is meant as a demonstration
how to build a coding agent yourself in C# using Semantic Kernel.

## High level requirements

- This project provides an interactive agent that can help you with coding
  tasks such as writing tests and implementing basic use cases.
- The agent must be able to work with a codebase on the user's computer
  without indexing the code base. We also don't require any tools beyond access
  to an LLM.

## Non-goals

- Build a production-ready coding agent
- We're not here to compete with other coding agents out there 
  (although it would be cool if we could beat Copilot)

## Quality goals

Since this is a learning project, we only provide a limited set of quality goals
that help guide the workshop. You shouldn't run this agent in production without
additional work.

| Quality Goal | Description                                                              |
| ------------ | ------------------------------------------------------------------------ |
| Q001         | The agent is reasonably safe to use under normal operational conditions. |
| Q002         | The agent is resistant against LLM provider failures.                    |
| Q003         | The agent is extensible so developers can improve its functionality      |

