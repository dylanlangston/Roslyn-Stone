"""LLM Provider integrations for chat functionality."""

from __future__ import annotations

import json
import os
from typing import TYPE_CHECKING, Any

if TYPE_CHECKING:
    from components.mcp_client import McpHttpClient

# Maximum iterations for tool calls to prevent infinite loops
MAX_TOOL_ITERATIONS = 10

# Default model for HuggingFace chat when none specified
DEFAULT_HUGGINGFACE_MODEL = "meta-llama/Llama-3.2-3B-Instruct"


def call_openai_chat(
    messages: list[dict[str, Any]],
    api_key: str,
    model: str,
    tools: list[dict[str, Any]],
    mcp_client: McpHttpClient,
) -> str:
    """Call OpenAI API with MCP tools.

    Args:
        messages: Chat message history.
        api_key: OpenAI API key.
        model: Model name to use.
        tools: Available MCP tools in OpenAI format.
        mcp_client: MCP client for tool calls.

    Returns:
        Response text from the model.
    """
    try:
        import openai

        client = openai.OpenAI(api_key=api_key)

        for _ in range(MAX_TOOL_ITERATIONS):
            response = client.chat.completions.create(
                model=model,
                messages=messages,  # type: ignore[arg-type]
                tools=tools if tools else None,  # type: ignore[arg-type]
            )

            message = response.choices[0].message

            # Handle tool calls
            if message.tool_calls:
                for tool_call in message.tool_calls:
                    tool_name = tool_call.function.name  # type: ignore[union-attr]
                    tool_args = json.loads(tool_call.function.arguments)  # type: ignore[union-attr]

                    # Call MCP tool
                    result = mcp_client.call_tool(tool_name, tool_args)

                    # Add tool result to messages
                    messages.append(
                        {
                            "role": "assistant",
                            "content": None,
                            "tool_calls": [
                                {
                                    "id": tool_call.id,
                                    "function": {
                                        "name": tool_name,
                                        "arguments": tool_call.function.arguments,  # type: ignore[union-attr]
                                    },
                                    "type": "function",
                                }
                            ],
                        }
                    )
                    messages.append(
                        {
                            "role": "tool",
                            "tool_call_id": tool_call.id,
                            "content": json.dumps(result),
                        }
                    )
                # Continue loop to make another call with tool results
                continue

            return message.content or "No response"

        return "Error: Maximum tool call iterations exceeded"
    except Exception as e:
        return f"Error: {e!s}"


def call_anthropic_chat(
    messages: list[dict[str, Any]],
    api_key: str,
    model: str,
    tools: list[dict[str, Any]],
    mcp_client: McpHttpClient,
) -> str:
    """Call Anthropic API with MCP tools.

    Args:
        messages: Chat message history.
        api_key: Anthropic API key.
        model: Model name to use.
        tools: Available MCP tools in Anthropic format.
        mcp_client: MCP client for tool calls.

    Returns:
        Response text from the model.
    """
    try:
        import anthropic

        client = anthropic.Anthropic(api_key=api_key)

        # Convert messages format
        anthropic_messages = []
        for msg in messages:
            if msg["role"] == "system":
                continue
            anthropic_messages.append({"role": msg["role"], "content": msg["content"]})

        for _ in range(MAX_TOOL_ITERATIONS):
            response = client.messages.create(
                model=model,
                max_tokens=4096,
                messages=anthropic_messages,  # type: ignore[arg-type]
                tools=tools if tools else None,  # type: ignore[arg-type]
            )

            # Handle tool calls
            if response.stop_reason == "tool_use":
                for content in response.content:
                    if content.type == "tool_use":
                        tool_name = content.name
                        tool_args = content.input

                        # Call MCP tool
                        result = mcp_client.call_tool(tool_name, tool_args)

                        # Add tool result and continue
                        anthropic_messages.append(
                            {"role": "assistant", "content": response.content}
                        )
                        anthropic_messages.append(
                            {
                                "role": "user",
                                "content": [
                                    {
                                        "type": "tool_result",
                                        "tool_use_id": content.id,
                                        "content": json.dumps(result),
                                    }
                                ],
                            }
                        )
                # Continue loop to make another call with tool results
                continue

            return response.content[0].text if response.content else "No response"  # type: ignore[union-attr]

        return "Error: Maximum tool call iterations exceeded"
    except Exception as e:
        return f"Error: {e!s}"


def call_gemini_chat(
    messages: list[dict[str, Any]],
    api_key: str,
    model: str,
    tools: list[dict[str, Any]],
    mcp_client: McpHttpClient,
) -> str:
    """Call Google Gemini API with MCP tools.

    Args:
        messages: Chat message history.
        api_key: Google API key.
        model: Model name to use.
        tools: Available MCP tools (currently unused).
        mcp_client: MCP client for tool calls (currently unused).

    Returns:
        Response text from the model.
    """
    try:
        import google.generativeai as genai

        genai.configure(api_key=api_key)

        model_instance = genai.GenerativeModel(model)

        # Convert messages to Gemini format
        prompt = "\n\n".join(
            [f"{msg['role']}: {msg['content']}" for msg in messages if msg.get("content")]
        )

        response = model_instance.generate_content(prompt)
        return response.text  # type: ignore[no-any-return]
    except Exception as e:
        return f"Error: {e!s}"


def call_huggingface_chat(
    messages: list[dict[str, Any]],
    api_key: str,
    model: str,
    mcp_client: McpHttpClient,
) -> str:
    """Call HuggingFace Inference API (supports serverless).

    Args:
        messages: Chat message history.
        api_key: HuggingFace API key (optional for serverless).
        model: Model name to use.
        mcp_client: MCP client (currently unused).

    Returns:
        Response text from the model.
    """
    try:
        from huggingface_hub import InferenceClient

        # Use provided key, or fall back to HF_API_KEY/HF_TOKEN from environment
        token: str | None = api_key
        if not token:
            token = os.environ.get("HF_API_KEY") or os.environ.get("HF_TOKEN")

        # Create client (serverless works without token, but rate limited)
        client = InferenceClient(token=token) if token else InferenceClient()

        # Convert messages to chat format
        chat_messages = []
        for msg in messages:
            if msg.get("content"):
                chat_messages.append({"role": msg["role"], "content": msg["content"]})

        # Call chat completion
        response: str = ""
        for message in client.chat_completion(
            messages=chat_messages,
            model=model if model else DEFAULT_HUGGINGFACE_MODEL,
            max_tokens=2048,
            stream=True,
        ):
            if message.choices and message.choices[0].delta.content:
                response += message.choices[0].delta.content

        return response if response else "No response"
    except Exception as e:
        return f"Error: {e!s}"
