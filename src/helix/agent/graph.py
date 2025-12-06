"""Define a LangGraph stateful graph with call_llm and call_tool nodes."""

import platform
from pathlib import Path
from typing import Dict, List, Literal, cast

import chevron
import tiktoken
from langchain_core.messages import AIMessage, BaseMessage, SystemMessage, trim_messages
from langchain_ollama import ChatOllama
from langgraph.graph import StateGraph
from langgraph.prebuilt import ToolNode

from helix.agent.state import InputState, State
from helix.agent.tools import TOOLS

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


# Define a new graph
builder = StateGraph(State, input_schema=InputState)

# Define the two nodes
builder.add_node("call_llm", call_llm)
builder.add_node("call_tool", ToolNode(TOOLS))

# Set the entrypoint as call_llm
# This means that this node is the first one called
builder.add_edge("__start__", "call_llm")


def should_call_tools(state: State) -> Literal["__end__", "call_tool"]:
    """
    Determine the next node based on whether the last message has tool calls.

    This function checks if the model's last message contains tool calls.

    Parameters
    ----------
    state : State
        The current state of the conversation.

    Returns
    -------
    Literal["__end__", "call_tool"]
        The name of the next node to call ("__end__" or "call_tool").
    """
    if not state.messages:
        return "__end__"

    last_message = state.messages[-1]

    if not isinstance(last_message, AIMessage):
        return "__end__"

    # If there are tool calls, go to call_tool node
    if last_message.tool_calls:
        return "call_tool"

    # Otherwise, end the turn
    return "__end__"


# Add a conditional edge to determine the next step after call_llm
builder.add_conditional_edges(
    "call_llm",
    # After call_llm finishes running, the next node(s) are scheduled
    # based on the output from should_call_tools
    should_call_tools,
)

# Add a normal edge from call_tool back to call_llm
# This creates a cycle: after using tools, we return to the model
builder.add_edge("call_tool", "call_llm")

# Compile the builder into an executable graph
graph = builder.compile(name="Agent")
