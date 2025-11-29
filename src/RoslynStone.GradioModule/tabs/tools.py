"""Tools tab for MCP tool execution."""

from __future__ import annotations

import json
from typing import TYPE_CHECKING, Any

import gradio as gr

if TYPE_CHECKING:
    from components.mcp_client import McpHttpClient


def create_tools_tab(
    mcp_client: McpHttpClient,
    tools_state: gr.State,
) -> tuple[
    gr.Button,
    gr.Markdown,
    gr.Dropdown,
    gr.Textbox,
    gr.Code,
    gr.Button,
    gr.Code,
]:
    """Create the Tools tab UI for executing MCP tools.

    Args:
        mcp_client: The MCP HTTP client instance
        tools_state: Gradio state for storing tools data

    Returns:
        Tuple of UI components for external event wiring
    """
    gr.Markdown("### Execute MCP Tools")
    gr.Markdown(
        "Tools perform operations like executing C# code, loading NuGet packages, etc."
    )

    refresh_tools_btn = gr.Button("🔄 Refresh Tools", size="sm")
    tools_status = gr.Markdown("Click 'Refresh Tools' to load available tools...")

    with gr.Row():
        with gr.Column(scale=1):
            tool_dropdown = gr.Dropdown(
                label="Select Tool", choices=[], interactive=True
            )
            tool_description = gr.Textbox(
                label="Tool Description",
                lines=5,
                interactive=False,
                max_lines=10,
            )
            tool_params_json = gr.Code(
                label="Tool Parameters (JSON)", language="json", value="{}", lines=10
            )
            execute_tool_btn = gr.Button("▶️ Execute Tool", variant="primary", size="lg")

        with gr.Column(scale=1):
            tool_result = gr.Code(
                label="Tool Result (JSON)", language="json", lines=20, interactive=False
            )

    # Tool examples with better formatting
    with gr.Accordion("📝 Example Tool Calls", open=True):
        gr.Markdown("### C# Code Execution Examples")

        gr.HTML("""
        <div style="background: rgba(18, 18, 26, 0.9); border: 1px solid rgba(0, 255, 249, 0.2); padding: 15px; border-radius: 8px; margin: 10px 0;">
            <h4 style="color: #00fff9; margin-bottom: 8px;">🔹 EvaluateCsharp - Execute simple C# code</h4>
            <pre style="background: #0a0a12; color: #e8e8e8; padding: 10px; border-radius: 4px; overflow-x: auto; border-left: 3px solid #00fff9;"><code>{
"code": "var x = 10; x * 2",
"createContext": false
}</code></pre>
        </div>

        <div style="background: rgba(18, 18, 26, 0.9); border: 1px solid rgba(0, 255, 249, 0.2); padding: 15px; border-radius: 8px; margin: 10px 0;">
            <h4 style="color: #00fff9; margin-bottom: 8px;">🔹 ValidateCsharp - Check syntax</h4>
            <pre style="background: #0a0a12; color: #e8e8e8; padding: 10px; border-radius: 4px; overflow-x: auto; border-left: 3px solid #ff00ff;"><code>{
"code": "var x = 10; x * 2"
}</code></pre>
        </div>

        <div style="background: rgba(18, 18, 26, 0.9); border: 1px solid rgba(0, 255, 249, 0.2); padding: 15px; border-radius: 8px; margin: 10px 0;">
            <h4 style="color: #00fff9; margin-bottom: 8px;">🔹 SearchNuGetPackages - Search packages</h4>
            <pre style="background: #0a0a12; color: #e8e8e8; padding: 10px; border-radius: 4px; overflow-x: auto; border-left: 3px solid #a855f7;"><code>{
"query": "json",
"skip": 0,
"take": 10
}</code></pre>
        </div>
        """)

    # Event handler functions
    def refresh_tools():
        """Refresh the list of available tools."""
        tools = mcp_client.list_tools()
        if not tools:
            return (
                "⚠️ No tools found or error connecting to server",
                gr.update(choices=[]),
                {},
            )

        tool_names = [t.get("name", "Unknown") for t in tools]
        tools_data = {t.get("name"): t for t in tools}

        return (
            f"✅ Loaded {len(tools)} tools",
            gr.update(choices=tool_names),
            tools_data,
        )

    def on_tool_selected(tool_name: str, tools_data: dict[str, Any]) -> tuple[str, str]:
        """When a tool is selected, show its description and input schema."""
        if not tool_name or not tools_data:
            return "", "{}"

        tool = tools_data.get(tool_name, {})
        description = tool.get("description", "No description available")

        # Extract input schema to help user understand parameters
        input_schema = tool.get("inputSchema", {})
        properties = input_schema.get("properties", {})
        required = input_schema.get("required", [])

        # Create both example and description in one pass
        example: dict[str, Any] = {}
        param_descriptions = []

        for prop_name, prop_info in properties.items():
            prop_desc = prop_info.get("description", "")
            prop_type = prop_info.get("type", "string")
            is_required = prop_name in required

            # Add placeholder values based on type
            if prop_type == "string":
                example[prop_name] = f"<{prop_name}>"
            elif prop_type in {"integer", "number"}:
                example[prop_name] = 0
            elif prop_type == "boolean":
                example[prop_name] = False
            elif prop_type == "array":
                example[prop_name] = []
            elif prop_type == "object":
                example[prop_name] = {}

            # Build description
            req_marker = "⚠️ REQUIRED" if is_required else "optional"
            param_descriptions.append(f"- `{prop_name}` ({req_marker}): {prop_desc}")

        example_json = json.dumps(example, indent=2)
        full_description = f"{description}\n\n**Parameters:**\n" + "\n".join(
            param_descriptions
        )

        return full_description, example_json

    def execute_tool(tool_name: str, params_json: str) -> str:
        """Execute the selected tool with given parameters."""
        if not tool_name:
            return json.dumps({"error": "Please select a tool"}, indent=2)

        try:
            params = json.loads(params_json) if params_json.strip() else {}
        except json.JSONDecodeError as e:
            return json.dumps({"error": f"Invalid JSON: {e!s}"}, indent=2)

        result = mcp_client.call_tool(tool_name, params)

        # Format result nicely
        return json.dumps(result, indent=2)

    # Wire up tool tab events
    refresh_tools_btn.click(
        fn=refresh_tools, outputs=[tools_status, tool_dropdown, tools_state]
    )

    tool_dropdown.change(
        fn=on_tool_selected,
        inputs=[tool_dropdown, tools_state],
        outputs=[tool_description, tool_params_json],
    )

    execute_tool_btn.click(
        fn=execute_tool, inputs=[tool_dropdown, tool_params_json], outputs=[tool_result]
    )

    return (
        refresh_tools_btn,
        tools_status,
        tool_dropdown,
        tool_description,
        tool_params_json,
        execute_tool_btn,
        tool_result,
    )
