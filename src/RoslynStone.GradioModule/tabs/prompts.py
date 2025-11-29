"""Prompts tab for viewing MCP prompts."""

from __future__ import annotations

import json
from typing import TYPE_CHECKING, Any

import gradio as gr

if TYPE_CHECKING:
    from components.mcp_client import McpHttpClient


def create_prompts_tab(
    mcp_client: McpHttpClient,
    prompts_state: gr.State,
) -> tuple[
    gr.Button,
    gr.Markdown,
    gr.Dropdown,
    gr.Textbox,
    gr.Button,
    gr.Textbox,
]:
    """Create the Prompts tab UI for viewing MCP prompts.

    Args:
        mcp_client: The MCP HTTP client instance
        prompts_state: Gradio state for storing prompts data

    Returns:
        Tuple of UI components for external event wiring
    """
    gr.Markdown("### View MCP Prompts")
    gr.Markdown(
        "Prompts provide guidance and examples for using the MCP server effectively."
    )

    refresh_prompts_btn = gr.Button("🔄 Refresh Prompts", size="sm")
    prompts_status = gr.Markdown("Click 'Refresh Prompts' to load available prompts...")

    with gr.Row():
        with gr.Column(scale=1):
            prompt_dropdown = gr.Dropdown(
                label="Select Prompt", choices=[], interactive=True
            )
            prompt_description = gr.Textbox(
                label="Prompt Description",
                lines=3,
                interactive=False,
                max_lines=5,
            )
            get_prompt_btn = gr.Button("📝 Get Prompt", variant="primary", size="lg")

        with gr.Column(scale=1):
            prompt_result = gr.Textbox(
                label="Prompt Content",
                lines=30,
                interactive=False,
                max_lines=50,
            )

    # Event handler functions
    def refresh_prompts() -> tuple[str, dict[str, Any], dict[str, Any]]:
        """Refresh the list of available prompts."""
        prompts = mcp_client.list_prompts()
        if not prompts:
            return (
                "⚠️ No prompts found or error connecting to server",
                gr.update(choices=[]),
                {},
            )

        prompt_names = [p.get("name", "Unknown") for p in prompts]
        prompts_data = {p.get("name"): p for p in prompts}

        return (
            f"✅ Loaded {len(prompts)} prompts",
            gr.update(choices=prompt_names),
            prompts_data,
        )

    def on_prompt_selected(prompt_name: str, prompts_data: dict[str, Any]) -> str:
        """When a prompt is selected, show its description."""
        if not prompt_name or not prompts_data:
            return ""

        prompt = prompts_data.get(prompt_name, {})
        return prompt.get("description", "No description available")

    def get_prompt(prompt_name: str) -> str:
        """Get the content of a prompt."""
        if not prompt_name:
            return "Please select a prompt"

        result = mcp_client.get_prompt(prompt_name)

        # Extract prompt messages
        if "messages" in result:
            messages = result["messages"]
            content_parts = []
            for msg in messages:
                role = msg.get("role", "unknown")
                content = msg.get("content", {})
                if isinstance(content, dict):
                    text = content.get("text", "")
                else:
                    text = str(content)
                content_parts.append(f"[{role.upper()}]\n{text}")
            return "\n\n".join(content_parts)

        return json.dumps(result, indent=2)

    # Wire up prompts tab events
    refresh_prompts_btn.click(
        fn=refresh_prompts, outputs=[prompts_status, prompt_dropdown, prompts_state]
    )

    prompt_dropdown.change(
        fn=on_prompt_selected,
        inputs=[prompt_dropdown, prompts_state],
        outputs=[prompt_description],
    )

    get_prompt_btn.click(
        fn=get_prompt, inputs=[prompt_dropdown], outputs=[prompt_result]
    )

    return (
        refresh_prompts_btn,
        prompts_status,
        prompt_dropdown,
        prompt_description,
        get_prompt_btn,
        prompt_result,
    )
