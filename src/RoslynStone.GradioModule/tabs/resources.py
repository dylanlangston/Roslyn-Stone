"""Resources tab for browsing MCP resources."""

from __future__ import annotations

import json
from typing import TYPE_CHECKING, Any

import gradio as gr

if TYPE_CHECKING:
    from components.mcp_client import McpHttpClient


def create_resources_tab(
    mcp_client: McpHttpClient,
    resources_state: gr.State,
) -> tuple[
    gr.Button,
    gr.Markdown,
    gr.Dropdown,
    gr.Textbox,
    gr.Textbox,
    gr.Button,
    gr.Code,
]:
    """Create the Resources tab UI for browsing MCP resources.

    Args:
        mcp_client: The MCP HTTP client instance
        resources_state: Gradio state for storing resources data

    Returns:
        Tuple of UI components for external event wiring
    """
    gr.Markdown("### Browse MCP Resources")
    gr.Markdown(
        "Resources provide read-only data like documentation and package search results."
    )

    refresh_resources_btn = gr.Button("🔄 Refresh Resources", size="sm")
    resources_status = gr.Markdown(
        "Click 'Refresh Resources' to load available resources..."
    )

    with gr.Row():
        with gr.Column(scale=1):
            resource_dropdown = gr.Dropdown(
                label="Select Resource Template", choices=[], interactive=True
            )
            resource_description = gr.Textbox(
                label="Resource Description",
                lines=5,
                interactive=False,
                max_lines=10,
            )
            resource_uri = gr.Textbox(
                label="Resource URI",
                placeholder="e.g., doc://System.String or nuget://search?q=json",
                lines=1,
            )
            read_resource_btn = gr.Button(
                "📖 Read Resource", variant="primary", size="lg"
            )

        with gr.Column(scale=1):
            resource_result = gr.Code(
                label="Resource Content", language="json", lines=20, interactive=False
            )

    # Resource examples with enhanced UI
    with gr.Accordion("📚 Example Resource URIs", open=True):
        gr.HTML("""
        <div style="background: rgba(18, 18, 26, 0.9); border: 1px solid rgba(0, 255, 249, 0.2); padding: 15px; border-radius: 8px; margin: 10px 0;">
            <h4 style="color: #00fff9; margin-bottom: 12px;">📖 Documentation Resources</h4>
            <ul style="list-style-type: none; padding-left: 0; margin: 0;">
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(0, 255, 249, 0.15); border-radius: 6px; border-left: 3px solid #00fff9;">
                    <code style="color: #00fff9; font-weight: bold;">doc://System.String</code> <span style="color: #c8c8d8;">- String class documentation</span>
                </li>
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(0, 255, 249, 0.15); border-radius: 6px; border-left: 3px solid #00fff9;">
                    <code style="color: #00fff9; font-weight: bold;">doc://System.Linq.Enumerable</code> <span style="color: #c8c8d8;">- LINQ methods</span>
                </li>
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(0, 255, 249, 0.15); border-radius: 6px; border-left: 3px solid #00fff9;">
                    <code style="color: #00fff9; font-weight: bold;">doc://System.Collections.Generic.List`1</code> <span style="color: #c8c8d8;">- List&lt;T&gt; docs</span>
                </li>
            </ul>
        </div>

        <div style="background: rgba(18, 18, 26, 0.9); border: 1px solid rgba(255, 0, 255, 0.2); padding: 15px; border-radius: 8px; margin: 10px 0;">
            <h4 style="color: #ff00ff; margin-bottom: 12px;">📦 NuGet Search Resources</h4>
            <ul style="list-style-type: none; padding-left: 0; margin: 0;">
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(255, 0, 255, 0.15); border-radius: 6px; border-left: 3px solid #ff00ff;">
                    <code style="color: #ff00ff; font-weight: bold;">nuget://search?q=json</code> <span style="color: #c8c8d8;">- Search JSON packages</span>
                </li>
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(255, 0, 255, 0.15); border-radius: 6px; border-left: 3px solid #ff00ff;">
                    <code style="color: #ff00ff; font-weight: bold;">nuget://search?q=http&take=5</code> <span style="color: #c8c8d8;">- Search HTTP packages (5 results)</span>
                </li>
            </ul>
        </div>

        <div style="background: rgba(18, 18, 26, 0.9); border: 1px solid rgba(168, 85, 247, 0.2); padding: 15px; border-radius: 8px; margin: 10px 0;">
            <h4 style="color: #c084fc; margin-bottom: 12px;">📦 Package Info Resources</h4>
            <ul style="list-style-type: none; padding-left: 0; margin: 0;">
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(168, 85, 247, 0.15); border-radius: 6px; border-left: 3px solid #c084fc;">
                    <code style="color: #c084fc; font-weight: bold;">nuget://packages/Newtonsoft.Json/versions</code> <span style="color: #c8c8d8;">- All versions</span>
                </li>
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(168, 85, 247, 0.15); border-radius: 6px; border-left: 3px solid #c084fc;">
                    <code style="color: #c084fc; font-weight: bold;">nuget://packages/Newtonsoft.Json/readme</code> <span style="color: #c8c8d8;">- Package README</span>
                </li>
            </ul>
        </div>

        <div style="background: rgba(18, 18, 26, 0.9); border: 1px solid rgba(0, 180, 255, 0.2); padding: 15px; border-radius: 8px; margin: 10px 0;">
            <h4 style="color: #00b4ff; margin-bottom: 12px;">⚙️ REPL State Resources</h4>
            <ul style="list-style-type: none; padding-left: 0; margin: 0;">
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(0, 180, 255, 0.15); border-radius: 6px; border-left: 3px solid #00b4ff;">
                    <code style="color: #00b4ff; font-weight: bold;">repl://state</code> <span style="color: #c8c8d8;">- Current REPL environment info</span>
                </li>
                <li style="margin: 8px 0; padding: 12px; background: #1a1a24; border: 1px solid rgba(0, 180, 255, 0.15); border-radius: 6px; border-left: 3px solid #00b4ff;">
                    <code style="color: #00b4ff; font-weight: bold;">repl://info</code> <span style="color: #c8c8d8;">- REPL capabilities</span>
                </li>
            </ul>
        </div>
        """)

    # Event handler functions
    def refresh_resources() -> tuple[str, dict[str, Any], dict[str, Any]]:
        """Refresh the list of available resources."""
        resources = mcp_client.list_resources()
        if not resources:
            return (
                "⚠️ No resources found or error connecting to server",
                gr.update(choices=[]),
                {},
            )

        resource_names = [r.get("name", "Unknown") for r in resources]
        resources_data = {r.get("name"): r for r in resources}

        return (
            f"✅ Loaded {len(resources)} resource templates",
            gr.update(choices=resource_names),
            resources_data,
        )

    def on_resource_selected(
        resource_name: str, resources_data: dict[str, Any]
    ) -> tuple[str, str]:
        """When a resource is selected, show its description and example URI."""
        if not resource_name or not resources_data:
            return "", ""

        resource = resources_data.get(resource_name, {})
        description = resource.get("description", "No description available")
        uri_template = resource.get("uriTemplate", resource.get("uri", ""))

        return description, uri_template

    def read_resource(uri: str) -> str:
        """Read a resource from the MCP server."""
        if not uri:
            return json.dumps({"error": "Please enter a resource URI"}, indent=2)

        result = mcp_client.read_resource(uri)

        # Format the result nicely
        if "contents" in result:
            # MCP resource response with contents array
            contents = result["contents"]
            if len(contents) == 1:
                content = contents[0]
                if content.get("mimeType") == "application/json" and "text" in content:
                    try:
                        parsed = json.loads(content["text"])
                        return json.dumps(parsed, indent=2)
                    except json.JSONDecodeError:
                        return content["text"]
                return content.get("text", json.dumps(content, indent=2))
            return json.dumps(contents, indent=2)

        return json.dumps(result, indent=2)

    # Wire up resource tab events
    refresh_resources_btn.click(
        fn=refresh_resources,
        outputs=[resources_status, resource_dropdown, resources_state],
    )

    resource_dropdown.change(
        fn=on_resource_selected,
        inputs=[resource_dropdown, resources_state],
        outputs=[resource_description, resource_uri],
    )

    read_resource_btn.click(
        fn=read_resource, inputs=[resource_uri], outputs=[resource_result]
    )

    return (
        refresh_resources_btn,
        resources_status,
        resource_dropdown,
        resource_description,
        resource_uri,
        read_resource_btn,
        resource_result,
    )
