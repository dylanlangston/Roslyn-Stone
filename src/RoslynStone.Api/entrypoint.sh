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

# Verify venv exists and has Gradio installed (should be pre-built in Docker image)
if [ ! -f /app/.venv/bin/python3 ] || ! /app/.venv/bin/python3 -c "import gradio" 2>/dev/null; then
    echo "WARNING: Virtual environment missing or incomplete. Attempting repair..."
    
    # Find the CSnakes redistributable Python interpreter
    PY_INTERP=$(ls /home/appuser/.config/CSnakes/python*/python/install/bin/python3 2>/dev/null | head -n1 || true)
    
    if [ -n "$PY_INTERP" ] && [ -x "$PY_INTERP" ]; then
        echo "Found Python interpreter: $PY_INTERP"
        
        # Recreate venv only if necessary
        if [ ! -f /app/.venv/bin/python3 ]; then
            echo "Recreating virtualenv at /app/.venv..."
            rm -rf /app/.venv
            "$PY_INTERP" -m venv /app/.venv
        fi
        
        # Install Gradio if missing
        if ! /app/.venv/bin/python3 -c "import gradio" 2>/dev/null; then
            echo "Installing Gradio..."
            # Try to use uv if available, otherwise fall back to pip
            if command -v uv &> /dev/null || [ -f /app/.venv/bin/uv ]; then
                UV_BIN=$(command -v uv 2>/dev/null || echo /app/.venv/bin/uv)
                $UV_BIN pip install "gradio>=5.0.0,<6.0.0" --python /app/.venv/bin/python3
            else
                /app/.venv/bin/pip install --quiet "gradio>=5.0.0,<6.0.0"
            fi
        fi
    else
        echo "ERROR: Python redistributable not found and venv is broken!"
        echo "Container may not function correctly."
    fi
fi

# Ensure symlink exists for backward compatibility
if [ ! -e /app/venv ] && [ -d /app/.venv ]; then
    ln -s /app/.venv /app/venv 2>/dev/null || true
fi

exec dotnet RoslynStone.Api.dll "$@"
