"""Tests for GUI interrupt functionality."""

import asyncio
from unittest.mock import MagicMock, patch

import pytest

from helix.gui import invoke_agent


@pytest.mark.asyncio
async def test_invoke_agent_handles_cancellation():
    """Test that invoke_agent handles CancelledError gracefully."""
    # Mock the graph to simulate a long-running operation
    with patch("helix.gui.graph") as mock_graph:
        # Create an async generator that we can cancel
        async def mock_stream():
            await asyncio.sleep(0.5)  # Simulate long operation
            yield {}

        mock_graph.astream = MagicMock(return_value=mock_stream())

        # Create a task for invoke_agent
        task = asyncio.create_task(invoke_agent("test prompt"))

        # Let it start
        await asyncio.sleep(0.1)

        # Cancel the task
        task.cancel()

        # Should raise CancelledError
        with pytest.raises(asyncio.CancelledError):
            await task


@pytest.mark.asyncio
async def test_invoke_agent_displays_interrupt_message_on_cancellation():
    """Test that invoke_agent displays an interrupt message when cancelled."""
    # Mock the graph to simulate a long-running operation
    with (
        patch("helix.gui.graph") as mock_graph,
        patch("helix.gui.console") as mock_console,
    ):
        # Create an async generator that we can cancel
        async def mock_stream():
            await asyncio.sleep(0.5)  # Simulate long operation
            yield {}

        mock_graph.astream = MagicMock(return_value=mock_stream())

        # Create a task for invoke_agent
        task = asyncio.create_task(invoke_agent("test prompt"))

        # Let it start
        await asyncio.sleep(0.1)

        # Cancel the task
        task.cancel()

        # Attempt to await and catch the CancelledError
        try:
            await task
        except asyncio.CancelledError:
            pass

        # Verify that the interrupt message was printed
        mock_console.print.assert_called_with("\n[yellow]Agent interrupted by user[/yellow]")


@pytest.mark.asyncio
async def test_invoke_agent_completes_normally_without_cancellation():
    """Test that invoke_agent completes normally when not cancelled."""
    from langchain_core.messages import AIMessage

    # Mock the graph to return a simple response
    with patch("helix.gui.graph") as mock_graph:
        # Create an async generator with a simple response
        async def mock_stream():
            yield {"call_llm": {"messages": [AIMessage(content="Test response")]}}

        mock_graph.astream = MagicMock(return_value=mock_stream())
        mock_graph.get_state = MagicMock(return_value=MagicMock(tasks=[]))

        # Run invoke_agent without cancellation - should complete normally
        await invoke_agent("test prompt")

        # Verify graph.astream was called
        assert mock_graph.astream.called
