"""Define a LangGraph stateful graph with call_llm and call_tool nodes."""

from pathlib import Path
from typing import Dict, List, Literal, cast

from langchain_core.messages import AIMessage, SystemMessage
from langchain_ollama import ChatOllama
from langgraph.graph import StateGraph
from langgraph.prebuilt import ToolNode

from helix.agent.state import InputState, State
from helix.agent.tools import TOOLS

# Load system instructions from markdown file
_SYSTEM_INSTRUCTIONS_PATH = Path(__file__).parent / "system_instructions.md"
_SYSTEM_INSTRUCTIONS = _SYSTEM_INSTRUCTIONS_PATH.read_text()


async def call_llm(state: State) -> Dict[str, List[AIMessage]]:
    """Call the LLM to generate a response.

    This function prepares the model with tool binding and processes the response.

    Args:
        state: The current state of the conversation.

    Returns:
        A dictionary containing the model's response message.
    """
    # Initialize the model with tool binding
    model = ChatOllama(model="qwen3-coder").bind_tools(TOOLS)

    # Prepend system instructions to the messages
    messages = [SystemMessage(content=_SYSTEM_INSTRUCTIONS), *state.messages]

    # Get the model's response
    response = cast(
        AIMessage,
        await model.ainvoke(messages),
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
    """Determine the next node based on whether the last message has tool calls.

    This function checks if the model's last message contains tool calls.

    Args:
        state: The current state of the conversation.

    Returns:
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
