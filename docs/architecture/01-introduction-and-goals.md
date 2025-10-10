# Introduction and goals

Helix is a console-based coding agent that is meant as a demonstration
how to build a coding agent yourself in C# using Semantic Kernel.

## High level requirements

- This project provides an interactive agent that can help you with coding
  tasks such as writing tests and implementing user stories.
- The agent must be able to work with a codebase on the user's computer
  without requiring tools beyond what is included with the installation of 
  the product.
- Users must also be able to deploy the agent to a central server and provide
  it with access to their sources stored in Azure DevOps, Gitlab or Github.

## Quality goals

Since this is a learning project, we only provide a limited set of quality goals
that help guide the workshop. You shouldn't run this agent in production without
additional work.

| Quality Goal | Description                                                                             |
| ------------ | --------------------------------------------------------------------------------------- |
| Q001         | The agent is reasonably safe to use under normal operational conditions.                |
| Q002         | The agent is resistant against LLM provider failures.                                   |
| Q003         | The agent is extensible through MCP servers so developers can improve its functionality |

