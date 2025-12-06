"""Test the agent's happy flow."""


import pytest
from langchain_core.messages import HumanMessage

from helix.agent.graph import THREAD_ID, _load_custom_instructions, graph


@pytest.mark.asyncio
async def test_agent_writes_haiku_about_ai():
    """Test that the agent can write a haiku about AI."""
    # Create the initial message
    messages = [HumanMessage(content="Write a haiku about AI")]
    config = {"configurable": {"thread_id": THREAD_ID}}

    # Invoke the graph with the message
    result = await graph.ainvoke({"messages": messages}, config)
    
    # Verify that we got a response
    assert "messages" in result
    assert len(result["messages"]) > 1  # Should have at least the input and output
    
    # Get the last message (should be from the AI)
    last_message = result["messages"][-1]
    
    # Verify it's an AI message
    assert hasattr(last_message, "content")
    assert last_message.content is not None
    assert len(last_message.content) > 0
    
    # Verify the response contains some text (haiku should have content)
    # We don't enforce strict haiku format, just that it responds
    assert isinstance(last_message.content, str)
    assert len(last_message.content.strip()) > 0


def test_load_custom_instructions_returns_none_when_file_missing(tmp_path, monkeypatch):
    """Test that _load_custom_instructions returns None when AGENTS.md doesn't exist."""
    monkeypatch.chdir(tmp_path)
    result = _load_custom_instructions()
    assert result is None


def test_load_custom_instructions_returns_content_when_file_exists(tmp_path, monkeypatch):
    """Test that _load_custom_instructions returns file contents when AGENTS.md exists."""
    agents_md = tmp_path / "AGENTS.md"
    agents_md.write_text("Custom instructions for the agent")
    monkeypatch.chdir(tmp_path)

    result = _load_custom_instructions()
    assert result == "Custom instructions for the agent"

