# AGENTS.md

This file provides comprehensive documentation for AI coding agents working with the Helix project backend. It focuses
on the ASP.NET Core application architecture and implementation details.

## Project Overview

Helix is a personal coding agent built with C# and Semantic Kernel. The project demonstrates how to build a fully 
functional coding agent that works with both local models and OpenAI models. The application features a web-based 
interface powered by React and communicates with the backend through SignalR for real-time interaction.

**Key Technologies:**

- .NET 9.0 with ASP.NET Core
- Microsoft Semantic Kernel v1.66.0 for agent orchestration
- Entity Framework Core with SQLite for data persistence
- SignalR for real-time communication
- Spectre.Console.Cli for command-line interface

## Project Structure

The backend solution is organized into the following directory structure:

```
src/Helix/
├── Agent/                    # Core agent implementation
│   ├── Plugins/             # Agent plugins (tools)
│   │   ├── Shell/          # Shell command execution
│   │   ├── TextEditor/     # File manipulation tools
│   │   └── SharedTools.cs  # Core agent tools
│   ├── Prompts/            # Agent instruction templates
│   ├── CodingAgent.cs      # Main agent orchestrator
│   ├── CodingAgentFactory.cs  # Implementation of the agent factory to create agent instances
│   ├── CodingAgentContext.cs  # The context information needed for the agent to learn about its surroundings
│   └── CodingAgentOptions.cs  # The options provided through the command line for the agent
├── Data/                    # Data layer
│   ├── Migrations/         # EF Core migrations
│   ├── ApplicationDbContext.cs  # The main application data context
│   ├── ConversationRepository.cs # Repository used to abstract queries
│   └── IConversationRepository.cs # Interface for the repository
├── Models/                  # Domain models
│   ├── Conversation.cs      # Tracks the context information for a single coding session
│   ├── Message.cs           # Base class for message projection
│   ├── AgentResponseMessage.cs # Projection of an agent response
│   ├── UserPromptMessage.cs  # Projection of a user prompt
│   ├── ToolCallMessage.cs    # Projection of a tool call
│   └── PendingFunctionCall.cs  # Tracks function calls awaiting user approval
├── Hubs/                    # SignalR hubs
│   └── CodingAgentHub.cs
├── Endpoints/               # Minimal API endpoints
│   └── GetConversationsEndpoint.cs
├── Commands/                # CLI commands
│   ├── RunAgentCommand.cs
│   └── RunAgentCommandSettings.cs
├── Services/                # Application services
│   ├── IUnitOfWork.cs
│   ├── UnitOfWork.cs
│   └── OpenDefaultBrowser.cs
├── Shared/                  # Shared utilities
│   └── EmbeddedResource.cs
├── ClientApp/               # React frontend (separate documentation)
├── Program.cs               # Application entry point
└── Helix.csproj            # Project file
```

## Architecture Overview

### Application Entry Point

**File:** `src/Helix/Program.cs`

The application uses Spectre.Console.Cli to provide a command-line interface. The entry point configures a `RunAgentCommand` as both the default command and the explicit "run" command.

```csharp
var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<RunAgentCommand>("run");
});
app.SetDefaultCommand<RunAgentCommand>();
return await app.RunAsync(args);
```

### Application Bootstrapping

**File:** `src/Helix/Commands/RunAgentCommand.cs`

The `RunAgentCommand` is responsible for:

1. **Setting up the working directory:** Creates a `.helix` directory in the target directory for storing the SQLite database
2. **Configuration:** Reads Azure OpenAI credentials from environment variables:
   - `AZURE_OPENAI_ENDPOINT` - Azure OpenAI resource endpoint
   - `AZURE_OPENAI_KEY` - Azure OpenAI resource key
   - `AZURE_OPENAI_DEPLOYMENT` - Azure OpenAI deployment name
3. **Dependency Injection:** Configures all services including:
   - Semantic Kernel with Azure OpenAI chat client
   - Entity Framework Core with SQLite
   - SignalR for real-time communication
   - Application services (UnitOfWork, ConversationRepository, CodingAgentFactory)
4. **Database Migration:** Automatically applies EF Core migrations on startup
5. **ASP.NET Core Pipeline:** Configures static files, CORS, SignalR hub, and fallback routing

### Core Agent Implementation

**File:** `src/Helix/Agent/CodingAgent.cs`

The `CodingAgent` class is the heart of Helix. It orchestrates the agent's interaction with the language model and tools.

**Key Characteristics:**

- **Transient Lifecycle:** Created for each coding task request
- **Maximum Iterations:** Configured to run up to 20 iterations (configurable via `MaxIterations` constant)
- **Plugin System:** Three main plugin categories are registered:
  1. `SharedTools` - Core agent tools including `final_output`
  2. `ShellPlugin` - System shell command execution
  3. `TextEditorPlugin` - File manipulation operations

**Core Methods:**

1. **`SubmitPromptAsync(string userPrompt, ICodingAgentCallbacks callbacks)`**
   - Entry point for processing user requests
   - Adds user message to chat history
   - Executes the agent loop
   - Resets the final tool output flag before starting

2. **`ApproveFunctionCall(string callId, ICodingAgentCallbacks callbacks)`**
   - Handles user approval of pending function calls
   - Executes the approved function and adds result to chat history
   - Continues agent loop if no more pending calls

3. **`DeclineFunctionCall(string callId, ICodingAgentCallbacks callbacks)`**
   - Handles user rejection of pending function calls
   - Removes the pending call without execution
   - Continues agent loop if no more pending calls

**Agent Loop Termination:**

The agent stops execution when:
1. The `final_output` tool is called by the agent
2. Maximum iterations (20) are reached
3. Cancellation is requested via CancellationToken

### Agent Plugins (Tools)

#### SharedTools Plugin

**File:** `src/Helix/Agent/Plugins/SharedTools.cs`

Provides the `final_output` tool that signals task completion:

```csharp
[KernelFunction("final_output")]
[Description("The final output tool MUST be called with final output to the user.")]
public void FinalToolOutput(string output)
```

This tool is **critical** for proper agent operation. When called, it sets `FinalToolOutputReady = true`, which stops the agent iteration loop.

#### ShellPlugin

**File:** `src/Helix/Agent/Plugins/Shell/ShellPlugin.cs`

Enables cross-platform shell command execution:

```csharp
[KernelFunction("shell")]
[Description("You can use the shell tool to execute any command.")]
public async Task<string> ExecuteCommandAsync(string command)
```

**Implementation Details:**
- Automatically selects `WindowsShell` or `UnixShell` based on OS
- Returns combined STDOUT and STDERR output
- Requires user permission before execution (see `RequiresPermission` method)

**Supporting Classes:**
- `IShell` - Interface for shell implementations
- `WindowsShell` - Windows PowerShell implementation
- `UnixShell` - Unix/Linux bash implementation
- `ShellCommandParser` - Parses shell commands
- `ParsedCommand` - Represents parsed command structure

#### TextEditorPlugin

**File:** `src/Helix/Agent/Plugins/TextEditor/TextEditorPlugin.cs`

Provides file manipulation capabilities:

1. **`view_file`** - Read file contents with optional line range support
2. **`write_file`** - Create new files (fails if file exists)
3. **`insert_text`** - Insert text at specific line numbers
4. **`replace_text`** - Replace text using regex pattern matching (ensures uniqueness)

All file operations use `FileLocation.Resolve()` for path resolution relative to the working directory.

### Data Layer

#### Database Context

**File:** `src/Helix/Data/ApplicationDbContext.cs`

The `ApplicationDbContext` manages data persistence using Entity Framework Core with SQLite.

**Configuration:**
- Database file location: `.helix/app.db` in the working directory
- Uses automatic migrations via `MigrateAsync()` on application startup
- Row versioning for optimistic concurrency control

**Entities:**

1. **Conversation**
   - Primary key: `Id` (Guid)
   - Properties: `Topic`, `ChatHistory`, `DateCreated`, `PendingFunctionCalls`
   - ChatHistory serialized as JSON to preserve tool call context
   - Uses custom `ValueComparer` for change detection

#### Repository Pattern

**File:** `src/Helix/Data/ConversationRepository.cs`

Implements `IConversationRepository` to provide:
- `FindByIdAsync(Guid id)` - Retrieve conversation by ID
- `InsertConversationAsync(Guid id)` - Create new conversation
- `UpdateConversationAsync(Conversation conversation)` - Update existing conversation
- `GetAllConversationsAsync()` - Retrieve all conversations

#### Unit of Work Pattern

**File:** `src/Helix/Services/UnitOfWork.cs`

Implements `IUnitOfWork` to coordinate database transactions:
- `SaveChangesAsync()` - Commits all pending changes to database

### Domain Models

#### Conversation Model

**File:** `src/Helix/Models/Conversation.cs`

Represents a conversation session containing:
- `Id` (Guid) - Unique identifier
- `Topic` (string) - Conversation title
- `ChatHistory` (ChatHistory) - Semantic Kernel chat history object
- `DateCreated` (DateTime) - Creation timestamp
- `PendingFunctionCalls` (List<PendingFunctionCall>) - Function calls awaiting approval

**Important:** `PendingFunctionCalls` tracks tool calls requiring user permission. When the list is empty, the agent continues execution automatically.

#### Message Models

**File:** `src/Helix/Models/Message.cs`

Abstract base class for message types. The application uses Semantic Kernel's built-in `ChatHistory` instead of
custom message entities, but maintains message-related types for API responses:

- `AgentResponseMessage` - Agent's text responses
- `UserPromptMessage` - User input messages
- `ToolCallMessage` - Tool invocation messages

These models are created as projections for the chat history that's stored in the database.
The model matches what the frontend expects.

### SignalR Communication

#### CodingAgentHub

**File:** `src/Helix/Hubs/CodingAgentHub.cs`

The SignalR hub connects the frontend to the coding agent. It inherits from `Hub<ICodingAgentCallbacks>` where `ICodingAgentCallbacks` defines the client-side methods.

**Server Methods (called by frontend):**

1. **`SubmitPrompt(Guid conversationId, string userPrompt)`**
   - Creates conversation if it doesn't exist
   - Creates agent instance via factory
   - Submits prompt for processing
   - Saves conversation changes to database

2. **`ApproveToolCall(Guid conversationId, string toolCallId)`**
   - Retrieves conversation
   - Approves specific function call
   - Continues agent execution
   - Saves changes

3. **`DeclineToolCall(Guid conversationId, string toolCallId)`**
   - Retrieves conversation
   - Declines specific function call
   - Continues agent execution
   - Saves changes

**Client Methods (defined in ICodingAgentCallbacks):**

These methods are called by the agent to stream responses back to the frontend in real-time.

**Hub Endpoint:** Mapped at `/hubs/coding` in `RunAgentCommand.cs`

### Minimal API Endpoints

#### GetConversationsEndpoint

**File:** `src/Helix/Endpoints/GetConversationsEndpoint.cs`

Provides REST endpoint to retrieve all conversations. Maps a minimal API endpoint using extension method pattern.

### Agent Prompts

**File:** `src/Helix/Agent/Prompts/Instructions.md`

Agent instructions are stored as an embedded resource and loaded via `EmbeddedResource.Read()`. The prompts use Handlebars template format compatible with Semantic Kernel's prompt template system.

**Note:** The file is marked as `<EmbeddedResource>` in `Helix.csproj` to include it in the compiled assembly.

## Build and Run Instructions

### Prerequisites

- .NET 9.0 SDK
- Node.js 20.x (for frontend)
- Azure OpenAI resource with configured deployment

### Environment Setup

Set the following environment variables:

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_KEY="your-api-key"
export AZURE_OPENAI_DEPLOYMENT="your-deployment-name"
```

### Building the Solution

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running the Application

```bash
cd src/Helix
dotnet run
```

The application will:
1. Create a `.helix` directory in the current working directory
2. Initialize the SQLite database at `.helix/app.db`
3. Apply any pending migrations
4. Start the web server on http://localhost:5000/
5. Automatically open the default browser

## Database Migrations

The project uses Entity Framework Core migrations:

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/Helix

# Apply migrations (automatically done on startup)
dotnet ef database update --project src/Helix

# Remove last migration
dotnet ef migrations remove --project src/Helix
```

**Note:** The application automatically applies migrations on startup via `dbContext.Database.MigrateAsync()` in `RunAgentCommand.cs`.

## Key Design Patterns

### Factory Pattern

**`CodingAgentFactory`** creates `CodingAgent` instances with proper dependency injection. It accepts a `Conversation` and returns a configured agent ready for use.

### Repository Pattern

**`ConversationRepository`** abstracts data access logic, making the codebase testable and maintainable.

### Unit of Work Pattern

**`UnitOfWork`** coordinates database transactions, ensuring data consistency across repository operations.

### Plugin Architecture

The agent uses Semantic Kernel's plugin system to expose tools. Plugins are:
- Registered using `_agentKernel.Plugins.AddFromObject(pluginInstance)`
- Discovered through `[KernelFunction]` attributes
- Described using `[Description]` attributes for the LLM

## Important Implementation Notes

### Chat History Persistence

The `ChatHistory` object is serialized as JSON in the database. This preserves tool calls and their context, which is critical for the agent to understand previous actions and results.

### Function Call Approval Flow

1. Agent requests tool execution
2. If tool requires permission (e.g., shell commands), it's added to `PendingFunctionCalls`
3. Frontend receives notification via SignalR
4. User approves or declines via SignalR hub method
5. Agent continues execution after user decision

### Concurrency Control

The `Conversation` entity uses row versioning (`IsRowVersion()`) to prevent concurrent modification conflicts when multiple users might interact with the same conversation.

### File Path Resolution

All file operations in `TextEditorPlugin` use `FileLocation.Resolve()` to ensure paths are resolved relative to the configured working directory (`CodingAgentContext.TargetDirectory`).

## Testing Considerations

When working with the codebase:

1. **Agent Tests:** Focus on the agent loop, tool invocation, and termination conditions
2. **Plugin Tests:** Verify tool behavior, especially cross-platform shell execution
3. **Repository Tests:** Test CRUD operations and concurrency handling
4. **Hub Tests:** Mock `ICodingAgentCallbacks` to test SignalR message flow

## Common Development Tasks

### Adding a New Plugin

1. Create plugin class in `src/Helix/Agent/Plugins/`
2. Add methods with `[KernelFunction]` and `[Description]` attributes
3. Register plugin in `CodingAgent` constructor: `_agentKernel.Plugins.AddFromObject(new YourPlugin())`
4. Update agent instructions if needed

### Modifying the Data Model

1. Update entity classes in `src/Helix/Models/`
2. Update `ApplicationDbContext` configuration in `OnModelCreating`
3. Add migration: `dotnet ef migrations add <MigrationName> --project src/Helix`
4. Test migration locally before committing

### Adding a New SignalR Method

1. Add method to `CodingAgentHub`
2. Add corresponding callback to `ICodingAgentCallbacks` interface
3. Update frontend to call/handle the new method

## Troubleshooting

### Agent Not Stopping

If the agent runs for 20 iterations without stopping:
- Verify the agent instructions prompt the LLM to call `final_output`
- Check if `SharedTools.FinalToolOutputReady` is being set
- Review chat history to see if tool calls are being made

### Database Migration Issues

If migrations fail:
- Delete `.helix/app.db` to start fresh
- Check `ApplicationDbContext` configuration
- Verify migration files in `Data/Migrations/`

### SignalR Connection Issues

If frontend can't connect:
- Verify hub is mapped: `app.MapHub<CodingAgentHub>("/hubs/coding")`
- Check CORS configuration
- Ensure SignalR is added: `builder.Services.AddSignalR()`

## Additional Resources

- **Semantic Kernel Documentation:** https://learn.microsoft.com/en-us/semantic-kernel/
- **Entity Framework Core:** https://learn.microsoft.com/en-us/ef/core/
- **SignalR:** https://learn.microsoft.com/en-us/aspnet/core/signalr/
- **Project README:** See `README.md` for user-facing documentation
- **Frontend Documentation:** Will be provided in separate documentation file

## Contributing Guidelines

When making changes to the backend:

1. Follow existing code style and naming conventions
2. Add XML documentation comments to public APIs
3. Update this documentation if architecture changes
4. Add tests for new functionality
5. Ensure all tests pass before committing
6. Keep changes focused and atomic

---

**Last Updated:** This documentation reflects the project state as of .NET 9.0 and Semantic Kernel 1.66.0.
