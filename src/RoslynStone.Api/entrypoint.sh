#!/bin/bash
set -e

# Check if running in Hugging Face Spaces
if [ -n "$SPACE_ID" ]; then
    echo "Detected Hugging Face Space environment. Switching to HTTP transport."
    export MCP_TRANSPORT=http
    # Default to port 7860 if not set
    if [ -z "$ASPNETCORE_URLS" ]; then
        export ASPNETCORE_URLS=http://+:7860
    fi
fi

# Find the CSnakes redistributable Python interpreter
PY_INTERP=$(ls /home/appuser/.config/CSnakes/python*/python/install/bin/python3 2>/dev/null | head -n1 || true)

if [ -n "$PY_INTERP" ] && [ -x "$PY_INTERP" ]; then
    echo "Found Python interpreter: $PY_INTERP"
    
    # If venv doesn't exist or is broken, recreate it using the redistributable Python
    if [ ! -f /app/.venv/bin/python3 ] || [ ! -f /app/.venv/bin/pip ]; then
        echo "Recreating virtualenv at /app/.venv..."
        rm -rf /app/.venv
        "$PY_INTERP" -m venv /app/.venv
        
        # Install uv into the venv for package management
        echo "Installing uv into venv..."
        /app/.venv/bin/pip install --quiet uv
        
        # Install gradio using uv (match pyproject.toml version)
        echo "Installing gradio..."
        cd /app && /app/.venv/bin/uv pip install -r /app/pyproject.toml --python /app/.venv/bin/python3 2>/dev/null || \
        /app/.venv/bin/uv pip install "gradio>=5.0.0,<6.0.0" --python /app/.venv/bin/python3
    fi
    
    # Create symlink for consistency
    if [ ! -e /app/venv ]; then
        ln -s /app/.venv /app/venv || true
    fi
else
    echo "Warning: Python redistributable not found, using pre-built venv"
    # Fallback: ensure symlink exists
    if [ ! -e /app/venv ]; then
        ln -s /app/.venv /app/venv || true
    fi
fi

exec dotnet RoslynStone.Api.dll "$@"
