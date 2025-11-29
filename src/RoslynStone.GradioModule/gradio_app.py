"""Interactive Gradio UI for Roslyn-Stone MCP Server.

Provides dynamic testing interface for MCP tools, resources, and prompts.
This is the main entry point - modular components are in subpackages.
"""

from __future__ import annotations

import atexit

import gradio as gr

from components.mcp_client import McpHttpClient
from tabs import (
    create_chat_tab,
    create_prompts_tab,
    create_resources_tab,
    create_setup_tab,
    create_tools_tab,
)
from theme.cyberpunk import get_cyberpunk_css
from utils.mcp_endpoint import get_mcp_endpoint_url


def create_landing_page(base_url: str | None = None) -> gr.Blocks:
    """Create the interactive Gradio UI for MCP server testing.

    Args:
        base_url: The base URL of the MCP server (e.g., http://localhost:7071)

    Returns:
        A Gradio Blocks interface
    """
    if base_url is None:
        base_url = "http://localhost:7071"

    # Initialize MCP client
    mcp_client = McpHttpClient(base_url)

    # Get the public MCP endpoint URL for display
    mcp_endpoint = get_mcp_endpoint_url()

    # Register cleanup to close the HTTP client on exit
    def cleanup() -> None:
        mcp_client.close()

    atexit.register(cleanup)

    # Get theme CSS
    custom_css = get_cyberpunk_css()

    # In Gradio 6.0+, Blocks() constructor simplified - theme and css moved to launch()
    with gr.Blocks(title="Roslyn-Stone MCP Testing UI") as demo:
        # Inject custom CSS via Markdown/HTML component instead
        gr.HTML(f"<style>{custom_css}</style>")

        # State management for storing tools, resources, and prompts data
        tools_state = gr.State({})
        resources_state = gr.State({})
        prompts_state = gr.State({})

        gr.Markdown(
            """
            # 🪨 Roslyn-Stone MCP Server - Interactive Testing UI

            Welcome to the **interactive testing interface** for Roslyn-Stone MCP Server.
            This UI dynamically loads all available tools, resources, and prompts from the MCP server.
            """
        )

        with gr.Tabs():
            # Setup Tab (Welcome + About consolidated)
            with gr.Tab("🚀 Setup"):
                create_setup_tab(mcp_endpoint, base_url)

            # Tools Tab
            with gr.Tab("🔧 Tools"):
                create_tools_tab(mcp_client, tools_state)

            # Resources Tab
            with gr.Tab("📚 Resources"):
                create_resources_tab(mcp_client, resources_state)

            # Prompts Tab
            with gr.Tab("💬 Prompts"):
                create_prompts_tab(mcp_client, prompts_state)

            # Chat Tab
            with gr.Tab("🤖 Chat"):
                create_chat_tab(mcp_client)

        gr.Markdown(
            """
            ---

            **Status**: 🟢 Connected to MCP server. Use the tabs above to explore and test available tools, resources, and prompts.
            """
        )

    return demo  # type: ignore[no-any-return]


def launch_app(
    base_url: str | None = None, server_port: int = 7860, share: bool = False
) -> gr.Blocks:
    """Launch the Gradio application.

    Args:
        base_url: The base URL of the MCP server
        server_port: Port to run Gradio on
        share: Whether to create a public link

    Returns:
        The running Gradio Blocks interface
    """
    demo = create_landing_page(base_url)
    demo.launch(server_port=server_port, share=share, server_name="127.0.0.1")
    return demo


if __name__ == "__main__":
    # Read configuration from environment
    import os

    base_url = os.environ.get("BASE_URL", "http://localhost:7071")
    server_port = int(os.environ.get("GRADIO_SERVER_PORT", "7860"))

    # Launch the app
    launch_app(base_url=base_url, server_port=server_port, share=False)
