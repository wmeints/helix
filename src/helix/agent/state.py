"""Define the state structures for the agent."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Sequence

from langchain_core.messages import AnyMessage
from langgraph.graph import add_messages
from typing_extensions import Annotated


@dataclass
class InputState:
    """
    Input state for the agent, representing a narrower interface to the outside world.

    This class is used to define the initial state and structure of incoming data.

    Attributes
    ----------
    messages : Annotated[Sequence[AnyMessage], add_messages]
        Messages tracking the primary execution state of the agent.
        The `add_messages` annotation ensures that new messages are merged with existing
        ones, updating by ID to maintain an "append-only" state unless a message with
        the same ID is provided.
    """

    messages: Annotated[Sequence[AnyMessage], add_messages] = field(
        default_factory=list
    )


@dataclass
class State(InputState):
    """
    Complete state of the agent, extending InputState with additional attributes.

    This class can be used to store any information needed throughout the agent's
    lifecycle.
    """

    # Additional attributes can be added here as needed.

