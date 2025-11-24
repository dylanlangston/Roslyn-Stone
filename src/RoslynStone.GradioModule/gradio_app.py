"""
Interactive Gradio UI for Roslyn-Stone MCP Server
Provides dynamic testing interface for MCP tools, resources, and prompts.
"""

import gradio as gr
import httpx
import json
from typing import Optional, Dict, List, Any, Tuple


# MCP Client for HTTP transport
class McpHttpClient:
    """Simple MCP HTTP client for interacting with the server"""
    
    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip('/')
        self.mcp_url = f"{self.base_url}/mcp"
        self.client = httpx.Client(timeout=30.0)
    
    def _send_request(self, method: str, params: Optional[Dict] = None) -> Dict[str, Any]:
        """Send a JSON-RPC request to the MCP server"""
        request_data = {
            "jsonrpc": "2.0",
            "id": 1,
            "method": method,
            "params": params or {}
        }
        
        try:
            response = self.client.post(self.mcp_url, json=request_data)
            response.raise_for_status()
            
            # MCP HTTP transport uses Server-Sent Events (SSE) format
            # Response format: "event: message\ndata: {json}\n\n"
            response_text = response.text
            
            # Parse SSE format
            if response_text.startswith("event:"):
                lines = response_text.strip().split('\n')
                for line in lines:
                    if line.startswith("data: "):
                        json_data = line[6:]  # Remove "data: " prefix
                        result = json.loads(json_data)
                        if "error" in result:
                            return {"error": result["error"]}
                        return result.get("result", {})
            else:
                # Fallback to regular JSON
                result = response.json()
                if "error" in result:
                    return {"error": result["error"]}
                return result.get("result", {})
                
        except Exception as e:
            return {"error": str(e)}
    
    def list_tools(self) -> List[Dict[str, Any]]:
        """List all available MCP tools"""
        result = self._send_request("tools/list")
        if "error" in result:
            return []
        return result.get("tools", [])
    
    def list_resources(self) -> List[Dict[str, Any]]:
        """List all available MCP resources"""
        result = self._send_request("resources/list")
        if "error" in result:
            return []
        return result.get("resources", [])
    
    def list_prompts(self) -> List[Dict[str, Any]]:
        """List all available MCP prompts"""
        result = self._send_request("prompts/list")
        if "error" in result:
            return []
        return result.get("prompts", [])
    
    def call_tool(self, name: str, arguments: Dict[str, Any]) -> Dict[str, Any]:
        """Call an MCP tool with given arguments"""
        return self._send_request("tools/call", {"name": name, "arguments": arguments})
    
    def read_resource(self, uri: str) -> Dict[str, Any]:
        """Read an MCP resource"""
        return self._send_request("resources/read", {"uri": uri})
    
    def get_prompt(self, name: str, arguments: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """Get an MCP prompt"""
        return self._send_request("prompts/get", {"name": name, "arguments": arguments or {}})


def create_landing_page(base_url: Optional[str] = None) -> gr.Blocks:
    """
    Create the interactive Gradio UI for MCP server testing.
    
    Args:
        base_url: The base URL of the MCP server (e.g., http://localhost:7071)
    
    Returns:
        A Gradio Blocks interface
    """
    if base_url is None:
        base_url = "http://localhost:7071"
    
    # Initialize MCP client
    mcp_client = McpHttpClient(base_url)
    
    # CSS for better styling
    custom_css = """
    .tool-card { 
        border: 1px solid #e0e0e0; 
        border-radius: 8px; 
        padding: 15px; 
        margin: 10px 0;
        background-color: #f9f9f9;
    }
    .param-input {
        margin: 5px 0;
    }
    .result-box {
        background-color: #f5f5f5;
        border-radius: 4px;
        padding: 10px;
        font-family: monospace;
        white-space: pre-wrap;
    }
    """
    
    with gr.Blocks(title="Roslyn-Stone MCP Testing UI", theme=gr.themes.Soft(), css=custom_css) as demo:
        gr.Markdown(
            """
            # 🪨 Roslyn-Stone MCP Server - Interactive Testing UI
            
            Welcome to the **interactive testing interface** for Roslyn-Stone MCP Server.
            This UI dynamically loads all available tools, resources, and prompts from the MCP server.
            """
        )
        
        with gr.Tabs():
            # Tools Tab
            with gr.Tab("🔧 Tools"):
                gr.Markdown("### Execute MCP Tools")
                gr.Markdown("Tools perform operations like executing C# code, loading NuGet packages, etc.")
                
                refresh_tools_btn = gr.Button("🔄 Refresh Tools", size="sm")
                tools_status = gr.Markdown("Click 'Refresh Tools' to load available tools...")
                
                with gr.Row():
                    with gr.Column(scale=1):
                        tool_dropdown = gr.Dropdown(
                            label="Select Tool",
                            choices=[],
                            interactive=True
                        )
                        tool_description = gr.Textbox(
                            label="Tool Description",
                            lines=3,
                            interactive=False
                        )
                        tool_params_json = gr.Code(
                            label="Tool Parameters (JSON)",
                            language="json",
                            value="{}",
                            lines=10
                        )
                        execute_tool_btn = gr.Button("▶️ Execute Tool", variant="primary")
                    
                    with gr.Column(scale=1):
                        tool_result = gr.Code(
                            label="Tool Result",
                            language="json",
                            lines=20,
                            interactive=False
                        )
                
                # Tool examples
                gr.Markdown("""
                #### Example Tool Calls
                
                **EvaluateCsharp** - Execute simple C# code:
                ```json
                {
                    "code": "var x = 10; x * 2",
                    "createContext": false
                }
                ```
                
                **ValidateCsharp** - Check syntax:
                ```json
                {
                    "code": "var x = 10; x * 2"
                }
                ```
                
                **SearchNuGetPackages** - Search packages:
                ```json
                {
                    "query": "json",
                    "skip": 0,
                    "take": 10
                }
                ```
                """)
                
                def refresh_tools():
                    """Refresh the list of available tools"""
                    tools = mcp_client.list_tools()
                    if not tools:
                        return "⚠️ No tools found or error connecting to server", gr.update(choices=[])
                    
                    tool_names = [t.get("name", "Unknown") for t in tools]
                    # Store tools data for later use
                    demo.tools_data = {t.get("name"): t for t in tools}
                    
                    return f"✅ Loaded {len(tools)} tools", gr.update(choices=tool_names)
                
                def on_tool_selected(tool_name):
                    """When a tool is selected, show its description and input schema"""
                    if not tool_name or not hasattr(demo, 'tools_data'):
                        return "", "{}"
                    
                    tool = demo.tools_data.get(tool_name, {})
                    description = tool.get("description", "No description available")
                    
                    # Extract input schema to help user understand parameters
                    input_schema = tool.get("inputSchema", {})
                    properties = input_schema.get("properties", {})
                    required = input_schema.get("required", [])
                    
                    # Create example JSON with parameter descriptions
                    example = {}
                    for prop_name, prop_info in properties.items():
                        prop_desc = prop_info.get("description", "")
                        prop_type = prop_info.get("type", "string")
                        is_required = prop_name in required
                        
                        # Add placeholder values based on type
                        if prop_type == "string":
                            example[prop_name] = f"<{prop_name}>"
                        elif prop_type == "integer" or prop_type == "number":
                            example[prop_name] = 0
                        elif prop_type == "boolean":
                            example[prop_name] = False
                        elif prop_type == "array":
                            example[prop_name] = []
                        elif prop_type == "object":
                            example[prop_name] = {}
                    
                    example_json = json.dumps(example, indent=2)
                    
                    full_description = f"{description}\n\n**Parameters:**\n"
                    for prop_name, prop_info in properties.items():
                        prop_desc = prop_info.get("description", "")
                        is_required = prop_name in required
                        req_marker = "⚠️ REQUIRED" if is_required else "optional"
                        full_description += f"- `{prop_name}` ({req_marker}): {prop_desc}\n"
                    
                    return full_description, example_json
                
                def execute_tool(tool_name, params_json):
                    """Execute the selected tool with given parameters"""
                    if not tool_name:
                        return json.dumps({"error": "Please select a tool"}, indent=2)
                    
                    try:
                        params = json.loads(params_json) if params_json.strip() else {}
                    except json.JSONDecodeError as e:
                        return json.dumps({"error": f"Invalid JSON: {str(e)}"}, indent=2)
                    
                    result = mcp_client.call_tool(tool_name, params)
                    return json.dumps(result, indent=2)
                
                # Wire up tool tab events
                refresh_tools_btn.click(
                    fn=refresh_tools,
                    outputs=[tools_status, tool_dropdown]
                )
                
                tool_dropdown.change(
                    fn=on_tool_selected,
                    inputs=[tool_dropdown],
                    outputs=[tool_description, tool_params_json]
                )
                
                execute_tool_btn.click(
                    fn=execute_tool,
                    inputs=[tool_dropdown, tool_params_json],
                    outputs=[tool_result]
                )
            
            # Resources Tab
            with gr.Tab("📚 Resources"):
                gr.Markdown("### Browse MCP Resources")
                gr.Markdown("Resources provide read-only data like documentation and package search results.")
                
                refresh_resources_btn = gr.Button("🔄 Refresh Resources", size="sm")
                resources_status = gr.Markdown("Click 'Refresh Resources' to load available resources...")
                
                with gr.Row():
                    with gr.Column(scale=1):
                        resource_dropdown = gr.Dropdown(
                            label="Select Resource Template",
                            choices=[],
                            interactive=True
                        )
                        resource_description = gr.Textbox(
                            label="Resource Description",
                            lines=3,
                            interactive=False
                        )
                        resource_uri = gr.Textbox(
                            label="Resource URI",
                            placeholder="e.g., doc://System.String or nuget://search?q=json",
                            lines=1
                        )
                        read_resource_btn = gr.Button("📖 Read Resource", variant="primary")
                    
                    with gr.Column(scale=1):
                        resource_result = gr.Code(
                            label="Resource Content",
                            language="json",
                            lines=20,
                            interactive=False
                        )
                
                # Resource examples
                gr.Markdown("""
                #### Example Resource URIs
                
                **Documentation:**
                - `doc://System.String` - String class documentation
                - `doc://System.Linq.Enumerable` - LINQ methods
                - `doc://System.Collections.Generic.List`1` - List<T> docs
                
                **NuGet Search:**
                - `nuget://search?q=json` - Search JSON packages
                - `nuget://search?q=http&take=5` - Search HTTP packages (5 results)
                
                **Package Info:**
                - `nuget://packages/Newtonsoft.Json/versions` - All versions
                - `nuget://packages/Newtonsoft.Json/readme` - Package README
                
                **REPL State:**
                - `repl://state` - Current REPL environment info
                - `repl://info` - REPL capabilities
                """)
                
                def refresh_resources():
                    """Refresh the list of available resources"""
                    resources = mcp_client.list_resources()
                    if not resources:
                        return "⚠️ No resources found or error connecting to server", gr.update(choices=[])
                    
                    resource_names = [r.get("name", "Unknown") for r in resources]
                    demo.resources_data = {r.get("name"): r for r in resources}
                    
                    return f"✅ Loaded {len(resources)} resource templates", gr.update(choices=resource_names)
                
                def on_resource_selected(resource_name):
                    """When a resource is selected, show its description and example URI"""
                    if not resource_name or not hasattr(demo, 'resources_data'):
                        return "", ""
                    
                    resource = demo.resources_data.get(resource_name, {})
                    description = resource.get("description", "No description available")
                    uri_template = resource.get("uriTemplate", resource.get("uri", ""))
                    
                    return description, uri_template
                
                def read_resource(uri):
                    """Read a resource from the MCP server"""
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
                                except:
                                    return content["text"]
                            return content.get("text", json.dumps(content, indent=2))
                        return json.dumps(contents, indent=2)
                    
                    return json.dumps(result, indent=2)
                
                # Wire up resource tab events
                refresh_resources_btn.click(
                    fn=refresh_resources,
                    outputs=[resources_status, resource_dropdown]
                )
                
                resource_dropdown.change(
                    fn=on_resource_selected,
                    inputs=[resource_dropdown],
                    outputs=[resource_description, resource_uri]
                )
                
                read_resource_btn.click(
                    fn=read_resource,
                    inputs=[resource_uri],
                    outputs=[resource_result]
                )
            
            # Prompts Tab
            with gr.Tab("💬 Prompts"):
                gr.Markdown("### View MCP Prompts")
                gr.Markdown("Prompts provide guidance and examples for using the MCP server effectively.")
                
                refresh_prompts_btn = gr.Button("🔄 Refresh Prompts", size="sm")
                prompts_status = gr.Markdown("Click 'Refresh Prompts' to load available prompts...")
                
                with gr.Row():
                    with gr.Column(scale=1):
                        prompt_dropdown = gr.Dropdown(
                            label="Select Prompt",
                            choices=[],
                            interactive=True
                        )
                        prompt_description = gr.Textbox(
                            label="Prompt Description",
                            lines=2,
                            interactive=False
                        )
                        get_prompt_btn = gr.Button("📝 Get Prompt", variant="primary")
                    
                    with gr.Column(scale=1):
                        prompt_result = gr.Textbox(
                            label="Prompt Content",
                            lines=25,
                            interactive=False
                        )
                
                def refresh_prompts():
                    """Refresh the list of available prompts"""
                    prompts = mcp_client.list_prompts()
                    if not prompts:
                        return "⚠️ No prompts found or error connecting to server", gr.update(choices=[])
                    
                    prompt_names = [p.get("name", "Unknown") for p in prompts]
                    demo.prompts_data = {p.get("name"): p for p in prompts}
                    
                    return f"✅ Loaded {len(prompts)} prompts", gr.update(choices=prompt_names)
                
                def on_prompt_selected(prompt_name):
                    """When a prompt is selected, show its description"""
                    if not prompt_name or not hasattr(demo, 'prompts_data'):
                        return ""
                    
                    prompt = demo.prompts_data.get(prompt_name, {})
                    description = prompt.get("description", "No description available")
                    
                    return description
                
                def get_prompt(prompt_name):
                    """Get the content of a prompt"""
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
                    fn=refresh_prompts,
                    outputs=[prompts_status, prompt_dropdown]
                )
                
                prompt_dropdown.change(
                    fn=on_prompt_selected,
                    inputs=[prompt_dropdown],
                    outputs=[prompt_description]
                )
                
                get_prompt_btn.click(
                    fn=get_prompt,
                    inputs=[prompt_dropdown],
                    outputs=[prompt_result]
                )
            
            # About Tab
            with gr.Tab("ℹ️ About"):
                gr.Markdown(
                    f"""
                    ## About Roslyn-Stone MCP Server
                    
                    **Server URL:** `{base_url}/mcp`
                    
                    This interactive UI allows you to:
                    - 🔧 **Execute MCP Tools** - Test C# code execution, package loading, and more
                    - 📚 **Browse Resources** - Access documentation and package information
                    - 💬 **View Prompts** - Get guidance on using the MCP server
                    
                    ### Connection Information
                    
                    This UI connects to the MCP server at `{base_url}/mcp` using HTTP transport.
                    
                    For integrating with Claude, VS Code, or other MCP clients, see the 
                    [GitHub repository](https://github.com/dylanlangston/Roslyn-Stone) for configuration instructions.
                    
                    ### What is Roslyn-Stone?
                    
                    Roslyn-Stone is a developer- and LLM-friendly C# sandbox for creating single-file 
                    utility programs through the Model Context Protocol (MCP). It helps AI systems create 
                    runnable C# programs using file-based apps with top-level statements.
                    
                    ### Features
                    
                    - **C# REPL** - Execute C# code with stateful sessions
                    - **NuGet Integration** - Search and load packages dynamically
                    - **Documentation** - Query .NET API documentation
                    - **Validation** - Check C# syntax before execution
                    - **MCP Protocol** - Standards-based AI integration
                    
                    ### Security Note
                    
                    ⚠️ This server can execute arbitrary C# code. Deploy with appropriate security measures:
                    - Run in isolated containers/sandboxes
                    - Implement authentication and rate limiting
                    - Restrict network access
                    - Monitor resource usage
                    
                    ### Links
                    
                    - [GitHub Repository](https://github.com/dylanlangston/Roslyn-Stone)
                    - [Model Context Protocol](https://github.com/modelcontextprotocol/specification)
                    - [Roslyn Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
                    """
                )
        
        gr.Markdown(
            """
            ---
            
            **Status**: 🟢 Connected to MCP server. Use the tabs above to explore and test available tools, resources, and prompts.
            """
        )
    
    return demo


def launch_app(base_url: Optional[str] = None, server_port: int = 7860, share: bool = False) -> gr.Blocks:
    """
    Launch the Gradio application.
    
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
