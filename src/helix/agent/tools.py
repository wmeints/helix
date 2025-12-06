"""Define tools for the agent to use."""

from langchain_core.tools import tool


@tool
def get_weather(location: str) -> str:
    """Get the current weather for a given location.
    
    Args:
        location: The city or location to get weather for.
        
    Returns:
        A string describing the weather conditions.
    """
    # This is a placeholder implementation
    return f"The weather in {location} is sunny and 72°F."


@tool
def calculate(expression: str) -> str:
    """Evaluate a mathematical expression.
    
    Args:
        expression: A mathematical expression to evaluate (e.g., "2 + 2").
        
    Returns:
        The result of the calculation as a string.
    """
    try:
        result = eval(expression)
        return f"The result of {expression} is {result}."
    except Exception as e:
        return f"Error calculating {expression}: {str(e)}"


# List of all available tools
TOOLS = [get_weather, calculate]

