"""Chat tab for AI-assisted conversations using MCP tools."""

from __future__ import annotations

from typing import TYPE_CHECKING

import gradio as gr

from utils.llm_providers import (
    call_anthropic_chat,
    call_gemini_chat,
    call_huggingface_chat,
    call_openai_chat,
)

if TYPE_CHECKING:
    from components.mcp_client import McpHttpClient


def create_chat_tab(mcp_client: McpHttpClient) -> tuple[
    gr.Dropdown,
    gr.Textbox,
    gr.Textbox,
    gr.Checkbox,
    gr.Button,
    gr.Chatbot,
    gr.Textbox,
    gr.Button,
]:
    """Create the Chat tab UI for AI-assisted conversations.

    Args:
        mcp_client: The MCP HTTP client instance

    Returns:
        Tuple of UI components for external event wiring
    """
    gr.Markdown("### Chat with AI using Roslyn-Stone MCP Tools")
    gr.Markdown("""
    Connect to various LLM providers and use Roslyn-Stone MCP tools in your conversations.

    **🚀 Free Option:** Use HuggingFace serverless inference (no API key needed, or use HF_API_KEY secret)

    ⚠️ **Security Note:** API keys are not stored. HF will use HF_API_KEY/HF_TOKEN secret if available.
    """)

    with gr.Row():
        with gr.Column(scale=1):
            # Provider selection
            provider = gr.Dropdown(
                label="LLM Provider",
                choices=[
                    "HuggingFace (Serverless)",
                    "OpenAI",
                    "Anthropic",
                    "Google Gemini",
                ],
                value="HuggingFace (Serverless)",
                interactive=True,
            )

            # API Key input (session state only, not stored)
            # For HF, will use HF_API_KEY/HF_TOKEN secret if blank
            api_key = gr.Textbox(
                label="API Key (optional for HF)",
                type="password",
                placeholder="Enter API key (or leave blank to use HF_API_KEY secret)",
                lines=1,
                info="Not stored - HF will use HF_API_KEY secret if blank",
            )

            # Model selection
            model = gr.Textbox(
                label="Model Name",
                value="meta-llama/Llama-3.2-3B-Instruct",
                placeholder="e.g., meta-llama/Llama-3.2-3B-Instruct, gpt-4o-mini",
                lines=1,
            )

            # Enable MCP tools
            enable_mcp = gr.Checkbox(
                label="Enable MCP Tools",
                value=True,
                info="Allow the AI to use Roslyn-Stone MCP tools",
            )

            # Clear button
            clear_btn = gr.Button("🗑️ Clear Chat", size="sm")

        with gr.Column(scale=2):
            # Chat interface (Gradio 6.0 removed 'type' parameter)
            chatbot = gr.Chatbot(label="Chat", height=500)

            # Message input
            msg = gr.Textbox(
                label="Message",
                placeholder="Ask the AI to help with C# code, NuGet packages, or .NET documentation...",
                lines=2,
            )

            send_btn = gr.Button("📤 Send", variant="primary", size="lg")

    # Chat examples
    gr.Markdown("""
    ### Example Prompts

    Try asking the AI to:
    - "Write a C# program that calculates the Fibonacci sequence"
    - "Search for JSON parsing NuGet packages"
    - "Show me documentation for System.Linq.Enumerable.Select"
    - "Create a C# script that reads a CSV file"
    - "Validate this C# code: var x = 10; Console.WriteLine(x);"

    The AI can use Roslyn-Stone MCP tools to execute C# code, search packages, and access documentation.
    """)

    # Event handler functions
    def chat_response(
        message: str,
        history: list,
        provider_name: str,
        key: str,
        model_name: str,
        use_mcp: bool,
    ) -> tuple[list, str]:
        """Handle chat messages and call appropriate LLM (keys not stored)."""
        if not message:
            return history, ""

        # Add user message to history (using messages format)
        history.append({"role": "user", "content": message})

        # Build messages for API
        messages = [
            {
                "role": "system",
                "content": "You are a helpful AI assistant with access to Roslyn-Stone MCP tools for C# development.",
            }
        ]
        for h in history:
            if h.get("role") and h.get("content"):
                messages.append({"role": h["role"], "content": h["content"]})

        # Get MCP tools if enabled (note: MCP tool calling only works with OpenAI/Anthropic)
        tools = []
        if use_mcp and provider_name in ["OpenAI", "Anthropic"]:
            mcp_tools = mcp_client.list_tools()
            for tool in mcp_tools:
                tools.append(
                    {
                        "type": "function",
                        "function": {
                            "name": tool.get("name"),
                            "description": tool.get("description", ""),
                            "parameters": tool.get("inputSchema", {}),
                        },
                    }
                )

        # Call appropriate provider (API key used only for this request, not stored)
        try:
            if provider_name == "OpenAI":
                if not key:
                    response_text = "Error: OpenAI API key is required"
                else:
                    response_text = call_openai_chat(
                        messages, key, model_name, tools, mcp_client
                    )
            elif provider_name == "Anthropic":
                if not key:
                    response_text = "Error: Anthropic API key is required"
                else:
                    response_text = call_anthropic_chat(
                        messages, key, model_name, tools, mcp_client
                    )
            elif provider_name == "Google Gemini":
                if not key:
                    response_text = "Error: Google API key is required"
                else:
                    response_text = call_gemini_chat(
                        messages, key, model_name, tools, mcp_client
                    )
            elif provider_name in {"HuggingFace (Serverless)", "HuggingFace"}:
                response_text = call_huggingface_chat(
                    messages, key, model_name, mcp_client
                )
            else:
                response_text = f"Error: Unknown provider {provider_name}"

            history.append({"role": "assistant", "content": response_text})
        except Exception as e:
            history.append({"role": "assistant", "content": f"Error: {e!s}"})

        return history, ""

    # Wire up chat events
    send_btn.click(
        fn=chat_response,
        inputs=[msg, chatbot, provider, api_key, model, enable_mcp],
        outputs=[chatbot, msg],
    )

    msg.submit(
        fn=chat_response,
        inputs=[msg, chatbot, provider, api_key, model, enable_mcp],
        outputs=[chatbot, msg],
    )

    clear_btn.click(fn=lambda: ([], ""), outputs=[chatbot, msg])

    return (
        provider,
        api_key,
        model,
        enable_mcp,
        clear_btn,
        chatbot,
        msg,
        send_btn,
    )
