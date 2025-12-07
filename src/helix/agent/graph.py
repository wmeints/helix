"""Define a LangGraph stateful graph with call_llm and call_tool nodes."""

import platform
from pathlib import Path
from typing import Any, Dict, List, Literal, cast

import chevron
import tiktoken
from langchain_core.messages import (
    AIMessage,
    BaseMessage,
    SystemMessage,
    ToolMessage,
    trim_messages,
)
from langchain_ollama import ChatOllama
from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import StateGraph
from langgraph.prebuilt import ToolNode
from langgraph.types import interrupt

from helix.agent.state import InputState, State
from helix.agent.tools import TOOLS, clear_todos

# Maximum context window size in tokens (128K)
MAX_CONTEXT_TOKENS = 128_000

# Load system instructions template from markdown file
_SYSTEM_INSTRUCTIONS_PATH = Path(__file__).parent / "system_instructions.md"
_SYSTEM_INSTRUCTIONS_TEMPLATE = _SYSTEM_INSTRUCTIONS_PATH.read_text()


def _render_system_instructions() -> str:
    """
    Render system instructions with environment context.

    Uses chevron to render the system instructions template with
    the current operating system and working directory.

    Returns
    -------
    str
        The rendered system instructions.
    """
    return chevron.render(
        _SYSTEM_INSTRUCTIONS_TEMPLATE,
        {
            "operating_system": platform.system(),
            "current_directory": str(Path.cwd()),
        },
    )


def _load_custom_instructions() -> str | None:
    """
    Load custom instructions from AGENTS.md if it exists.

    Returns
    -------
    str or None
        The contents of AGENTS.md if it exists, None otherwise.
    """
    agents_md_path = Path.cwd() / "AGENTS.md"

    if agents_md_path.exists():
        return agents_md_path.read_text()

    return None


def _count_tokens(messages: list[BaseMessage]) -> int:
    """
    Count the number of tokens in a list of messages.

    Uses tiktoken with cl100k_base encoding for token counting.
    This is an approximation for non-OpenAI models but provides
    a reasonable estimate for context window management.

    Parameters
    ----------
    messages : list[BaseMessage]
        List of messages to count tokens for.

    Returns
    -------
    int
        The total number of tokens across all messages.
    """
    encoding = tiktoken.get_encoding("cl100k_base")
    total_tokens = 0

    for message in messages:
        content = message.content
        if isinstance(content, str):
            total_tokens += len(encoding.encode(content))
        elif isinstance(content, list):
            for item in content:
                if isinstance(item, str):
                    total_tokens += len(encoding.encode(item))
                elif isinstance(item, dict) and "text" in item:
                    total_tokens += len(encoding.encode(item["text"]))

    return total_tokens


async def call_llm(state: State) -> Dict[str, List[AIMessage]]:
    """
    Call the LLM to generate a response.

    This function prepares the model with tool binding and processes the response.
    Messages are trimmed to fit within the 128K token context window.

    Parameters
    ----------
    state : State
        The current state of the conversation.

    Returns
    -------
    Dict[str, List[AIMessage]]
        A dictionary containing the model's response message.
    """
    # Initialize the model with tool binding
    model = ChatOllama(model="qwen3-coder").bind_tools(TOOLS)

    # Build the messages list with system instructions
    system_messages = [SystemMessage(content=_render_system_instructions())]

    # Add custom instructions if AGENTS.md exists
    custom_instructions = _load_custom_instructions()

    if custom_instructions:
        system_messages.append(SystemMessage(content=custom_instructions))

    messages = [*system_messages, *state.messages]

    # Trim messages to fit within the context window
    trimmed_messages = trim_messages(
        messages,
        max_tokens=MAX_CONTEXT_TOKENS,
        token_counter=_count_tokens,
        strategy="last",
        start_on="human",
        include_system=True,
        allow_partial=False,
    )

    # Get the model's response
    response = cast(
        AIMessage,
        await model.ainvoke(trimmed_messages),
    )

    # Return the model's response as a list to be added to existing messages
    return {"messages": [response]}


def check_tool_approval(state: State) -> Dict[str, Any]:
    """
    Check if tool calls require user approval.

    This function interrupts execution for each tool call, allowing the GUI
    to check permissions and prompt the user if needed.

    Parameters
    ----------
    state : State
        The current state of the conversation.

    Returns
    -------
    Dict[str, Any]
        An empty dict if all tools were approved, or messages with decline
        responses for any declined tools.
    """
    if not state.messages:
        return {}

    last_message = state.messages[-1]

    if not isinstance(last_message, AIMessage):
        return {}

    if not last_message.tool_calls:
        return {}

    declined_messages: List[ToolMessage] = []

    for tool_call in last_message.tool_calls:
        tool_name = tool_call["name"]
        tool_args = tool_call["args"]

        # Interrupt and wait for approval decision from GUI
        approval = interrupt(
            {
                "type": "tool_approval",
                "tool_name": tool_name,
                "tool_args": tool_args,
                "tool_call_id": tool_call["id"],
            }
        )

        if not approval.get("approved", False):
            # Tool was declined - add a tool message indicating the decline
            reason = approval.get("reason", "declined by user")
            declined_messages.append(
                ToolMessage(
                    content=f"Tool '{tool_name}' {reason}.",
                    tool_call_id=tool_call["id"],
                    name=tool_name,
                )
            )

    if declined_messages:
        return {"messages": declined_messages}

    return {}


# Define a new graph
builder = StateGraph(State, input_schema=InputState)

# Define the nodes
builder.add_node("call_llm", call_llm)
builder.add_node("check_tool_approval", check_tool_approval)
builder.add_node("call_tool", ToolNode(TOOLS))

# Set the entrypoint as call_llm
# This means that this node is the first one called
builder.add_edge("__start__", "call_llm")


def should_call_tools(state: State) -> Literal["__end__", "check_tool_approval"]:
    """
    Determine the next node based on whether the last message has tool calls.

    This function checks if the model's last message contains tool calls.
    All tool calls are routed through approval.

    Parameters
    ----------
    state : State
        The current state of the conversation.

    Returns
    -------
    Literal["__end__", "check_tool_approval"]
        The name of the next node to call.
    """
    if not state.messages:
        return "__end__"

    last_message = state.messages[-1]

    if not isinstance(last_message, AIMessage):
        return "__end__"

    if last_message.tool_calls:
        return "check_tool_approval"

    return "__end__"


# Add a conditional edge to determine the next step after call_llm
builder.add_conditional_edges(
    "call_llm",
    # After call_llm finishes running, the next node(s) are scheduled
    # based on the output from should_call_tools
    should_call_tools,
)

def should_execute_tools(state: State) -> Literal["call_tool", "call_llm"]:
    """
    Determine if tools should be executed after approval check.

    If the last message is a ToolMessage with a decline, skip tool execution
    and return to the LLM. Otherwise, proceed with tool execution.

    Parameters
    ----------
    state : State
        The current state of the conversation.

    Returns
    -------
    Literal["call_tool", "call_llm"]
        The name of the next node to call.
    """
    if not state.messages:
        return "call_tool"

    last_message = state.messages[-1]

    # If the last message is a tool decline message, go back to LLM
    if isinstance(last_message, ToolMessage):
        content = str(last_message.content)
        if "declined by user" in content or "denied by settings" in content:
            return "call_llm"

    return "call_tool"


# Add conditional edge from tool approval check
builder.add_conditional_edges("check_tool_approval", should_execute_tools)

# Add a normal edge from call_tool back to call_llm
# This creates a cycle: after using tools, we return to the model
builder.add_edge("call_tool", "call_llm")

# Create a memory checkpointer for state persistence
checkpointer = MemorySaver()

# Compile the builder into an executable graph with checkpointing
graph = builder.compile(name="Agent", checkpointer=checkpointer)

# Default thread ID for the conversation
THREAD_ID = "helix-main"


def clear_conversation() -> None:
    """
    Clear the agent's conversation history and todo list.

    Resets the messages in the graph state to an empty list and
    clears all todo items from memory.
    """
    config = {"configurable": {"thread_id": THREAD_ID}}
    graph.update_state(config, {"messages": []})
    clear_todos()
