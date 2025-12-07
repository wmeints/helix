# Introduction and Goals

## Purpose

Helix is a coding agent built with Python that assists developers with software
engineering tasks. It uses local LLMs through Ollama for inference, providing a
privacy-friendly and cost-effective alternative to cloud-based coding assistants.

## Goals

| Priority | Goal                        | Description                                                                |
| -------- | --------------------------- | -------------------------------------------------------------------------- |
| 1        | Local-first operation       | Run entirely on local hardware without requiring cloud API keys            |
| 2        | Terminal-native experience  | Provide a rich terminal interface for seamless developer workflow          |
| 3        | Tool-based interaction      | Enable the agent to read/write files and execute shell commands            |
| 4        | User control over execution | Require explicit approval for tool calls to maintain user trust and safety |

## Stakeholders

| Role       | Expectations                                                         |
| ---------- | -------------------------------------------------------------------- |
| Developer  | Fast, accurate coding assistance without sharing code with the cloud |
| Maintainer | Simple, well-structured codebase that is easy to extend              |

## Quality Goals

| Priority | Quality Goal  | Scenario                                              |
| -------- | ------------- | ----------------------------------------------------- |
| 1        | Security      | User must approve all tool calls before execution     |
| 2        | Usability     | Terminal interface should be intuitive and responsive |
| 3        | Extensibility | Adding new tools should require minimal code changes  |
