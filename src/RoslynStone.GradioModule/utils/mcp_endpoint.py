"""MCP endpoint URL resolution utilities."""

from __future__ import annotations

import os


def get_mcp_endpoint_url() -> str:
    """Get the public MCP endpoint URL for clients to connect to.

    On HuggingFace Spaces, this returns the embed URL.
    Otherwise, returns the configured base URL.

    Returns:
        The MCP endpoint URL string.
    """
    # Check for HuggingFace Space
    space_id = os.environ.get("SPACE_ID")
    if space_id:
        # Format: username/repo -> username-repo.hf.space
        space_subdomain = space_id.replace("/", "-").lower()
        return f"https://{space_subdomain}.hf.space/mcp"

    # Check for custom BASE_URL
    base_url = os.environ.get("BASE_URL")
    if base_url:
        return f"{base_url.rstrip('/')}/mcp"

    # Check for ASPNETCORE_URLS
    aspnetcore_urls = os.environ.get("ASPNETCORE_URLS")
    if aspnetcore_urls:
        first_url = aspnetcore_urls.split(";")[0]
        # Replace wildcards with localhost for display
        first_url = first_url.replace("http://+:", "http://localhost:")
        first_url = first_url.replace("http://*:", "http://localhost:")
        first_url = first_url.replace("http://0.0.0.0:", "http://localhost:")
        return f"{first_url.rstrip('/')}/mcp"

    return "http://localhost:7071/mcp"
