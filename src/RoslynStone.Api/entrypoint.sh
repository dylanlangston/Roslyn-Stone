#!/bin/bash
set -e

# Check if running in Hugging Face Spaces
if [ -n "$SPACE_ID" ]; then
    echo "Detected Hugging Face Space environment. Switching to HTTP transport."
    export MCP_TRANSPORT=http
    # Default to port 7860 if not set (HF Spaces requirement)
    if [ -z "$ASPNETCORE_URLS" ]; then
        export ASPNETCORE_URLS=http://+:7860
    fi
fi

exec dotnet RoslynStone.Api.dll "$@"
