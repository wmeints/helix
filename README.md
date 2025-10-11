# Helix

Welcome to my personal coding agent project. I'm building this project to help me learn more about coding agents and how
to build one in C# with Semantic Kernel. The goal is to provide a fully functional agent that you can use with local
models and the classic OpenAI models as well.

It's not that I don't like the agents that are available today, but I feel that it is helpful to understand the
internals of an agent to be effective at using one in your daily job. And who doesn't love building something cool?

## Getting started

This application has a web-based interface that you use to interact with the agent. You run the agent from the project
directory you're working in. For now, you'll need to follow these steps to run the agent:

### Cloning the repository

```shell
git clone https://github.com/wmeints/helix
```

### Setting up environment variables

Set the following environment variables in the shell of choice.

| Environment variable      | Description                                         |
|---------------------------|-----------------------------------------------------|
| `AZURE_OPENAI_ENDPOINT`   | The endpoint for your Azure OpenAI resource.        |
| `AZURE_OPENAI_KEY`        | The key for your Azure OpenAI resource.             |
| `AZURE_OPENAI_DEPLOYMENT` | The deployment name for your Azure OpenAI resource. |

### Running the agent

You can run the agent like this from the working directory that you want to work on:

```shell
dotnet run --project <path-to-project>/src/Helix/Helix.csproj
```

-------------------------

**Note:** This will not be the final command. I'll provide a proper installation package later.

-------------------------

The agent will automatically open the default browser to the web-based interface.
If it doesn't open, please find it at http://localhost:5000/




