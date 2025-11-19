# Docker Deployment Guide

This document describes how to build and run the Roslyn-Stone MCP server in a Docker container with Gradio landing page support.

## Prerequisites

- Docker Engine 20.10+
- Internet connection for downloading Python redistributable and packages

## Building the Image

```bash
# From repository root
docker build -t roslyn-stone -f src/RoslynStone.Api/Dockerfile .
```

The build process:
1. **Build Stage**: Compiles .NET application and generates CSnakes bindings
2. **Publish Stage**: 
   - Installs CSnakes.Stage tool
   - Downloads Python 3.12 redistributable via `setup-python`
   - Installs UV (fast Python package manager)
   - Installs Gradio 6.x into virtual environment
   - Publishes .NET application
3. **Runtime Stage**: 
   - Copies published app, CSnakes Python, and venv
   - Sets up environment variables for Python/UV
   - Runs as non-root user (`appuser`)

## Running the Container

### Stdio Mode (Default)

```bash
docker run -i --rm roslyn-stone
```

Stdio mode doesn't expose any ports and communicates via stdin/stdout. Gradio landing page is **not activated** in this mode.

### HTTP Mode (with Gradio Landing Page)

```bash
docker run -d \
  -e MCP_TRANSPORT=http \
  -e ASPNETCORE_URLS=http://+:8080 \
  -p 8080:8080 \
  --name roslyn-stone \
  roslyn-stone
```

Access:
- **Gradio Landing Page**: http://localhost:8080/
- **MCP Endpoint**: http://localhost:8080/mcp
- **Health Checks**: http://localhost:8080/health

## Environment Variables

### MCP Server Configuration

- `MCP_TRANSPORT`: `stdio` (default) or `http`
- `ASPNETCORE_URLS`: Listening URLs for HTTP mode (e.g., `http://+:8080`)
- `DOTNET_ENVIRONMENT`: `Production` (default), `Development`, `Staging`

### Python/CSnakes Configuration

These are set automatically in the Dockerfile:

- `LD_LIBRARY_PATH`: Path to libpython (`/home/appuser/.config/CSnakes/python3.12.9/python/install/lib`)
- `PATH`: Includes UV binary path (`/home/appuser/.local/bin`)

### Gradio Configuration

- `GRADIO_SERVER_PORT`: Port for Gradio (default: 7860, internal only)

## CSnakes and UV Integration

The Dockerfile uses:

1. **CSnakes.Stage** (`setup-python`):
   - Downloads Python 3.12.9 redistributable
   - Creates isolated Python environment
   - No system Python dependencies required

2. **UV Package Manager**:
   - Installs Python packages 10-100x faster than pip
   - Resolves dependencies efficiently
   - Caches packages for faster rebuilds

3. **Virtual Environment**:
   - Isolated Python environment in `.venv`
   - Pre-installed Gradio 6.x
   - Copied to runtime image

## Docker Compose Example

```yaml
version: '3.8'

services:
  roslyn-stone:
    build:
      context: .
      dockerfile: src/RoslynStone.Api/Dockerfile
    environment:
      - MCP_TRANSPORT=http
      - ASPNETCORE_URLS=http://+:8080
    ports:
      - "8080:8080"
    restart: unless-stopped
```

## Troubleshooting

### Gradio Not Starting

If Gradio doesn't start in HTTP mode:

1. Check logs: `docker logs roslyn-stone`
2. Verify environment: `MCP_TRANSPORT=http` and `ASPNETCORE_URLS` are set
3. Check Python environment: `docker exec roslyn-stone ls -la .venv/`

### Python Import Errors

If you see Python-related errors:

1. Verify CSnakes Python is present: `docker exec roslyn-stone ls -la /home/appuser/.config/CSnakes/`
2. Check LD_LIBRARY_PATH: `docker exec roslyn-stone printenv LD_LIBRARY_PATH`

### Port Already in Use

```bash
# Find process using port
docker ps | grep 8080

# Stop existing container
docker stop roslyn-stone
docker rm roslyn-stone
```

## Multi-Platform Builds

To build for multiple platforms:

```bash
docker buildx build --platform linux/amd64,linux/arm64 \
  -t roslyn-stone:latest \
  -f src/RoslynStone.Api/Dockerfile .
```

## Security Considerations

- Container runs as non-root user (`appuser`)
- No elevated privileges required
- Python environment is isolated
- Only necessary ports are exposed

## Size Optimization

The image includes:
- .NET 10 runtime (~200 MB)
- CSnakes Python redistributable (~50 MB)
- Gradio and dependencies (~150 MB)

Total image size: ~400-500 MB

To reduce size:
- Use multi-stage builds (already implemented)
- Remove development tools from runtime image
- Use Alpine-based images (requires additional setup for CSnakes)
