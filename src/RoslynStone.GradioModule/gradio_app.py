"""
Gradio Landing Page for Roslyn-Stone MCP Server
Provides instructions on how to connect to the MCP server.
"""

import gradio as gr
from typing import Optional


def create_landing_page(base_url: Optional[str] = None) -> gr.Blocks:
    """
    Create the Gradio landing page with MCP connection instructions.
    
    Args:
        base_url: The base URL of the MCP server (e.g., http://localhost:7071)
    
    Returns:
        A Gradio Blocks interface
    """
    if base_url is None:
        base_url = "http://localhost:7071"
    
    with gr.Blocks(title="Roslyn-Stone MCP Server", theme=gr.themes.Soft()) as demo:
        gr.Markdown(
            """
            # 🪨 Roslyn-Stone MCP Server
            
            Welcome to **Roslyn-Stone** - A developer- and LLM-friendly C# REPL service that brings 
            the power of the Roslyn compiler to AI coding assistants through the Model Context Protocol (MCP).
            """
        )
        
        with gr.Tabs():
            with gr.Tab("Quick Start"):
                gr.Markdown(
                    f"""
                    ## Connect to This Server
                    
                    This MCP server is running in **HTTP mode** and is accessible at:
                    
                    ```
                    {base_url}/mcp
                    ```
                    
                    ### For Claude Desktop / Claude Code / VS Code / Cursor
                    
                    Add this to your MCP configuration file:
                    
                    **Linux/macOS**: `~/.config/Code/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json`  
                    **Windows**: `%APPDATA%\\Code\\User\\globalStorage\\saoudrizwan.claude-dev\\settings\\cline_mcp_settings.json`
                    
                    ```json
                    {{
                        "mcpServers": {{
                            "roslyn-stone": {{
                                "url": "{base_url}/mcp",
                                "transport": "http"
                            }}
                        }}
                    }}
                    ```
                    
                    ### For Docker/Stdio Mode
                    
                    Prefer running via Docker with stdio transport? Use:
                    
                    ```json
                    {{
                        "mcpServers": {{
                            "roslyn-stone": {{
                                "command": "docker",
                                "args": [
                                    "run", "-i", "--rm",
                                    "-e", "DOTNET_USE_POLLING_FILE_WATCHER=1",
                                    "ghcr.io/dylanlangston/roslyn-stone:latest"
                                ]
                            }}
                        }}
                    }}
                    ```
                    """
                )
            
            with gr.Tab("Features"):
                gr.Markdown(
                    """
                    ## What Can It Do?
                    
                    ### 🎯 Core Features
                    
                    - **C# REPL via Roslyn Scripting** - Execute C# code with optional stateful sessions
                    - **Context Management** - Maintain variables and state across executions
                    - **Real-time Compile Error Reporting** - Get detailed compilation errors and warnings
                    - **Documentation Access** - Query .NET type/method docs via `doc://` resource URIs
                    - **NuGet Integration** - Search packages via `nuget://` resources, load with tools
                    
                    ### 🔧 MCP Tools Available
                    
                    **REPL Tools:**
                    - `EvaluateCsharp` - Execute C# code in a REPL session
                    - `ValidateCsharp` - Validate C# syntax and semantics
                    - `ResetRepl` - Reset REPL sessions
                    - `GetReplInfo` - Get REPL environment information
                    
                    **NuGet Tools:**
                    - `LoadNuGetPackage` - Load NuGet packages into REPL
                    - `SearchNuGetPackages` - Search for NuGet packages
                    - `GetNuGetPackageVersions` - Get all versions of a package
                    - `GetNuGetPackageReadme` - Get package README content
                    
                    **Documentation Tools:**
                    - `GetDocumentation` - Get XML documentation for .NET types/methods
                    
                    ### 📚 MCP Resources
                    
                    - `doc://{symbolName}` - .NET XML documentation
                    - `nuget://search?q={query}` - Search NuGet packages
                    - `nuget://packages/{id}/versions` - Get package versions
                    - `nuget://packages/{id}/readme` - Get package README
                    - `repl://state` - REPL information
                    - `repl://sessions` - Active REPL sessions
                    """
                )
            
            with gr.Tab("Examples"):
                gr.Markdown(
                    """
                    ## Usage Examples
                    
                    ### Execute C# Code
                    
                    ```
                    User: "Create a variable x = 10"
                    Assistant: [Calls EvaluateCsharp] → Returns contextId
                    
                    User: "Multiply x by 2"
                    Assistant: [Calls EvaluateCsharp with contextId] → Returns 20
                    ```
                    
                    ### Query Documentation
                    
                    ```
                    User: "Show me documentation for System.String"
                    Assistant: [Reads doc://System.String resource]
                    ```
                    
                    ### Search and Load Packages
                    
                    ```
                    User: "Search for JSON parsing packages"
                    Assistant: [Reads nuget://search?q=json resource]
                    
                    User: "Load Newtonsoft.Json"
                    Assistant: [Calls LoadNuGetPackage tool]
                    ```
                    
                    ### Complex LINQ Query
                    
                    ```csharp
                    var numbers = Enumerable.Range(1, 100);
                    var result = numbers
                        .Where(n => n % 2 == 0)
                        .Select(n => n * n)
                        .Sum();
                    ```
                    """
                )
            
            with gr.Tab("About"):
                gr.Markdown(
                    """
                    ## About Roslyn-Stone
                    
                    Named as a playful nod to the Rosetta Stone—the ancient artifact that helped decode 
                    languages—Roslyn-Stone helps AI systems decode and execute C# code seamlessly through 
                    the Model Context Protocol (MCP).
                    
                    ### Links
                    
                    - [GitHub Repository](https://github.com/dylanlangston/Roslyn-Stone)
                    - [Model Context Protocol](https://github.com/modelcontextprotocol/specification)
                    - [Roslyn Scripting APIs](https://learn.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting)
                    
                    ### Security Considerations
                    
                    ⚠️ **Important**: This is a code execution service. Deploy with appropriate security measures:
                    
                    - Run in isolated containers/sandboxes
                    - Implement rate limiting
                    - Add authentication/authorization
                    - Restrict network access
                    - Monitor resource usage
                    - Use read-only file systems where possible
                    
                    ### License
                    
                    See the [LICENSE](https://github.com/dylanlangston/Roslyn-Stone/blob/main/LICENSE) file for details.
                    """
                )
        
        gr.Markdown(
            """
            ---
            
            **Status**: 🟢 Server is running and ready to accept MCP connections.
            
            Visit the [GitHub repository](https://github.com/dylanlangston/Roslyn-Stone) for more information.
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
