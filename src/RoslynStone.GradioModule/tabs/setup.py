"""Setup Tab - Welcome page with connection instructions and project info."""

from __future__ import annotations

import gradio as gr


def create_setup_tab(mcp_endpoint: str, base_url: str | None = None) -> None:
    """Create the Setup/Welcome tab content.

    Consolidates welcome information, setup instructions, features overview,
    and security notes into a single comprehensive tab.

    Args:
        mcp_endpoint: The MCP server endpoint URL to display.
        base_url: The base URL of the MCP server (for display purposes).
    """
    # Use base_url if provided for additional context, otherwise derive from mcp_endpoint
    server_url = base_url if base_url else mcp_endpoint.replace("/mcp", "")

    gr.Markdown(
        f"""
        ## Welcome to Roslyn-Stone MCP Server!

        **Roslyn-Stone** is a developer- and LLM-friendly C# sandbox for creating single-file
        utility programs through the [Model Context Protocol (MCP)](https://github.com/modelcontextprotocol/specification).
        It helps AI systems create runnable C# programs using file-based apps with top-level statements.

        **Server URL:** `{server_url}/mcp`

        ---

        ### 🔗 MCP Server Endpoint

        Connect your MCP client to this server using the following endpoint:

        ```
        {mcp_endpoint}
        ```

        ---

        ### ✨ Features

        | Feature | Description |
        |---------|-------------|
        | **C# REPL** | Execute C# code with stateful sessions |
        | **NuGet Integration** | Search and load packages dynamically |
        | **Documentation** | Query .NET API documentation |
        | **Validation** | Check C# syntax before execution |
        | **MCP Protocol** | Standards-based AI integration |

        ---

        ### 📋 Quick Setup Instructions

        #### Claude Desktop

        Add to your `claude_desktop_config.json`:

        ```json
        {{
          "mcpServers": {{
            "roslyn-stone": {{
              "command": "npx",
              "args": [
                "mcp-remote",
                "{mcp_endpoint}"
              ]
            }}
          }}
        }}
        ```

        #### VS Code with Copilot

        Add to your VS Code `settings.json`:

        ```json
        {{
          "github.copilot.chat.mcpServers": {{
            "roslyn-stone": {{
              "type": "http",
              "url": "{mcp_endpoint}"
            }}
          }}
        }}
        ```

        #### Using mcp-remote (Any MCP Client)

        If your client doesn't support HTTP transport directly, use `mcp-remote` as a bridge:

        ```bash
        npx mcp-remote {mcp_endpoint}
        ```

        ---

        ### 🛠️ Available Capabilities

        | Category | Description |
        |----------|-------------|
        | **🔧 Tools** | Execute C# code, validate syntax, search NuGet packages, load assemblies |
        | **📚 Resources** | Access .NET documentation, NuGet package info, REPL state |
        | **💬 Prompts** | Get guidance and best practices for C# development |
        | **🤖 Chat** | Interactive chat with AI using MCP tools (try the Chat tab!) |

        ---

        ### 🔒 Security Note

        ⚠️ This server can execute arbitrary C# code. When self-hosting:
        - Run in isolated containers or sandboxes
        - Implement authentication and rate limiting
        - Restrict network access as needed
        - Monitor resource usage

        ---

        ### 🔗 Links

        - [GitHub Repository](https://github.com/dylanlangston/Roslyn-Stone)
        - [Model Context Protocol](https://github.com/modelcontextprotocol/specification)
        - [Roslyn Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)

        ---

        **Explore the tabs above to test tools, browse resources, view prompts, or chat with AI!**
        """
    )
