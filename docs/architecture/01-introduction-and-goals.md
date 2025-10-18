# Introduction and goals

Helix is a coding agent that focuses on providing a local open-source experience
for writing code. 

## Project goal

Provide an open-source coding agent experience with a local web interface so
developers can use it without having to take out a subscription with one of
the big providers.

## High level requirements

- Developers can use local models with the agent to build code. 
- Developers can extend the agent with their custom prompts stored in markdown.
- Developers can extend the behavior of the agent with MCP servers.

## Quality goals

- The agent asks for permission for potential destructive commands.
- The agent automatically retries model calls when they fail due to quota
  limitations or other transient problems.

