# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Helix is a personal coding agent project built with C# and Semantic Kernel. The goal is to provide a fully functional agent that works with both local models and OpenAI models. The project features a web-based interface for interacting with the agent.

## Technology Stack

- **Backend**: .NET 9.0 with ASP.NET Core
- **Agent Framework**: Microsoft Semantic Kernel (v1.66.0)
- **Database**: SQLite with Entity Framework Core
- **Frontend**: React 19 with TypeScript, Vite, and Tailwind CSS
- **Real-time Communication**: SignalR
- **Testing**: xUnit

## Environment Setup

Required environment variables:

- `AZURE_OPENAI_ENDPOINT` - Azure OpenAI resource endpoint
- `AZURE_OPENAI_KEY` - Azure OpenAI resource key
- `AZURE_OPENAI_DEPLOYMENT` - Azure OpenAI deployment name

## Build and Run Commands

### Building the Solution

```bash
dotnet restore
dotnet build --configuration Release --no-restore
```

### Running Tests

```bash
# Run all tests
dotnet test --no-build --configuration Release --verbosity normal

# Run tests with build
dotnet test
```

### Running the Application

```bash
dotnet run --project src/Helix/Helix.csproj
```

The application will open at http://localhost:5000/

### Frontend Development

```bash
cd src/Helix/ClientApp

# Install dependencies
npm ci

# Run development server (Vite)
npm run dev

# Build frontend
npm run build

# Lint
npm run lint

# Lint and fix
npm run lint:fix
```

## Architecture

### Agent Core (`CodingAgent`)

The `CodingAgent` class (src/Helix/Agent/CodingAgent.cs) is the core of Helix. It:

- Uses Semantic Kernel's ChatCompletionAgent with Handlebars prompt templates
- Implements a loop-based execution model with a maximum of 20 iterations
- Manages chat history using ChatHistoryAgentThread
- Provides streaming responses via `InvokeStreamingAsync`
- Supports cancellation through CancellationTokenSource

The agent stops execution when either:
1. The `final_output` tool is called (via SharedTools)
2. Maximum iterations (20) are reached
3. Cancellation is requested

### Agent Plugins

The agent has access to three plugin categories:

1. **SharedTools** (src/Helix/Agent/Plugins/SharedTools.cs)
   - `final_output` - Signals completion and provides summary to user
   - Required to properly stop the agent iteration loop

2. **ShellPlugin** (src/Helix/Agent/Plugins/Shell/)
   - Cross-platform shell command execution
   - Automatically selects WindowsShell or UnixShell based on OS
   - Returns combined STDOUT and STDERR

3. **TextEditorPlugin** (src/Helix/Agent/Plugins/TextEditor/)
   - `view_file` - Read file contents with line range support
   - `write_file` - Create new files
   - `insert_text` - Insert text at specific line numbers
   - `replace_text` - Replace text using regex (ensures uniqueness)

All file operations use `FileLocation.Resolve()` for path resolution.

### Data Layer

The application uses Entity Framework Core with SQLite:

- **ApplicationDbContext** (src/Helix/Data/ApplicationDbContext.cs) manages:
  - Conversations - User conversation sessions
  - Messages - Table-Per-Hierarchy (TPH) inheritance for:
    - UserMessage
    - AssistantResponse
    - ToolCallMessage

- Database file location: `.helix/app.db` in the working directory
- Uses automatic migrations with `EnsureCreated()`
- Row versioning for concurrency control

### Frontend Architecture

- **Framework**: React 19 with TypeScript
- **Build Tool**: Vite
- **UI Components**: Radix UI primitives with Tailwind CSS
- **Forms**: React Hook Form with Zod validation
- **Markdown**: react-markdown with remark-gfm and rehype-raw
- **Real-time**: @microsoft/signalr for agent communication

The frontend is integrated with ASP.NET Core using SpaProxy:
- Development: Vite dev server on http://localhost:5173/
- Production: Built to wwwroot via npm run build

### Prompts

Agent instructions are stored as embedded resources in src/Helix/Agent/Prompts/Instructions.txt and loaded via `EmbeddedResource.Read()`.

## Database Migrations

The project uses Entity Framework Core migrations:

```bash
# Add a migration
dotnet ef migrations add <MigrationName> --project src/Helix

# Update database (note: currently using EnsureCreated in Program.cs)
dotnet ef database update --project src/Helix
```

## CI/CD

GitHub Actions workflow (.github/workflows/ci.yml) runs on push/PR to main:
1. Sets up .NET 9.0 and Node.js 20
2. Installs frontend dependencies
3. Restores .NET dependencies
4. Builds solution in Release configuration
5. Runs all tests
