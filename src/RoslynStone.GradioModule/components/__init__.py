"""Roslyn-Stone Gradio UI Components.

This package contains modular components for the Gradio interface:
- mcp_client: HTTP client for MCP server communication
- tabs: UI tab components (setup, tools, resources, prompts, chat)
"""

from components.mcp_client import McpHttpClient

__all__ = ["McpHttpClient"]
