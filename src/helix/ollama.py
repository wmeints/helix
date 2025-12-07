"""Ollama connectivity and model availability checks."""

import json
import urllib.error
import urllib.request
from dataclasses import dataclass

# Default Ollama API base URL
OLLAMA_BASE_URL = "http://localhost:11434"

# Required model name
REQUIRED_MODEL = "qwen3-coder"


@dataclass
class OllamaStatus:
    """
    Status of Ollama connectivity and model availability.

    Attributes
    ----------
    is_running : bool
        Whether Ollama is running and reachable.
    model_available : bool
        Whether the required model is available.
    available_models : list[str]
        List of available model names.
    error_message : str | None
        Error message if any check failed.
    """

    is_running: bool
    model_available: bool
    available_models: list[str]
    error_message: str | None = None


def check_ollama_status(
    base_url: str = OLLAMA_BASE_URL,
    required_model: str = REQUIRED_MODEL,
) -> OllamaStatus:
    """
    Check if Ollama is running and the required model is available.

    Parameters
    ----------
    base_url : str, optional
        The base URL for the Ollama API (default: http://localhost:11434).
    required_model : str, optional
        The model name to check for (default: qwen3-coder).

    Returns
    -------
    OllamaStatus
        Status containing connectivity and model availability info.
    """
    try:
        # Try to list available models
        request = urllib.request.Request(
            f"{base_url}/api/tags",
            method="GET",
        )

        with urllib.request.urlopen(request, timeout=5) as response:
            data = json.loads(response.read().decode("utf-8"))

        # Extract model names from response
        models = data.get("models", [])
        available_models = []

        for model in models:
            model_name = model.get("name", "")
            # Ollama returns names like "qwen3-coder:latest", we need to match the base name
            base_name = model_name.split(":")[0]
            available_models.append(base_name)

        # Check if required model is available
        model_available = required_model in available_models

        return OllamaStatus(
            is_running=True,
            model_available=model_available,
            available_models=available_models,
        )

    except urllib.error.URLError as e:
        # Connection refused or network error
        return OllamaStatus(
            is_running=False,
            model_available=False,
            available_models=[],
            error_message=f"Cannot connect to Ollama: {e.reason}",
        )
    except TimeoutError:
        return OllamaStatus(
            is_running=False,
            model_available=False,
            available_models=[],
            error_message="Connection to Ollama timed out",
        )
    except json.JSONDecodeError:
        return OllamaStatus(
            is_running=True,
            model_available=False,
            available_models=[],
            error_message="Invalid response from Ollama API",
        )
