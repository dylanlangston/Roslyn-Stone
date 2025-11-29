"""MCP HTTP Client for Roslyn-Stone server communication."""

from __future__ import annotations

import json
from typing import Any

import httpx


class McpHttpClient:
    """Simple MCP HTTP client for interacting with the server."""

    def __init__(self, base_url: str) -> None:
        """Initialize the MCP HTTP client.

        Args:
            base_url: Base URL of the MCP server.
        """
        self.base_url = base_url.rstrip("/")
        self.mcp_url = f"{self.base_url}/mcp"
        self.client = httpx.Client(timeout=30.0)

    def close(self) -> None:
        """Close the HTTP client and release resources."""
        self.client.close()

    def __enter__(self) -> McpHttpClient:
        """Enter context manager."""
        return self

    def __exit__(self, exc_type: object, exc_val: object, exc_tb: object) -> None:
        """Exit context manager."""
        self.close()

    def _send_request(self, method: str, params: dict | None = None) -> dict[str, Any]:
        """Send a JSON-RPC request to the MCP server."""
        request_data = {"jsonrpc": "2.0", "id": 1, "method": method, "params": params or {}}

        try:
            response = self.client.post(self.mcp_url, json=request_data)
            response.raise_for_status()

            # MCP HTTP transport uses Server-Sent Events (SSE) format
            # Response format: "event: message\ndata: {json}\n\n"
            response_text = response.text

            # Parse SSE format
            if response_text.startswith("event:"):
                lines = response_text.strip().split("\n")
                for line in lines:
                    if line.startswith("data: "):
                        json_data = line[6:]  # Remove "data: " prefix
                        result = json.loads(json_data)
                        if "error" in result:
                            return {"error": result["error"]}
                        return result.get("result", {})  # type: ignore[no-any-return]
            else:
                # Fallback to regular JSON
                result = response.json()
                if "error" in result:
                    return {"error": result["error"]}
                return result.get("result", {})  # type: ignore[no-any-return]

        except (httpx.HTTPError, json.JSONDecodeError) as e:
            return {"error": str(e)}
        except Exception as e:
            # Re-raise system-exiting exceptions
            if isinstance(e, (KeyboardInterrupt, SystemExit)):
                raise
            return {"error": str(e)}

    def list_tools(self) -> list[dict[str, Any]]:
        """List all available MCP tools."""
        result = self._send_request("tools/list")
        if "error" in result:
            return []
        tools: list[dict[str, Any]] = result.get("tools", [])
        return tools

    def list_resources(self) -> list[dict[str, Any]]:
        """List all available MCP resource templates."""
        result = self._send_request("resources/templates/list")
        if "error" in result:
            return []
        templates: list[dict[str, Any]] = result.get("resourceTemplates", [])
        return templates

    def list_prompts(self) -> list[dict[str, Any]]:
        """List all available MCP prompts."""
        result = self._send_request("prompts/list")
        if "error" in result:
            return []
        prompts: list[dict[str, Any]] = result.get("prompts", [])
        return prompts

    def call_tool(self, name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        """Call an MCP tool with given arguments."""
        return self._send_request("tools/call", {"name": name, "arguments": arguments})

    def read_resource(self, uri: str) -> dict[str, Any]:
        """Read an MCP resource."""
        return self._send_request("resources/read", {"uri": uri})

    def get_prompt(self, name: str, arguments: dict[str, Any] | None = None) -> dict[str, Any]:
        """Get an MCP prompt."""
        return self._send_request("prompts/get", {"name": name, "arguments": arguments or {}})
