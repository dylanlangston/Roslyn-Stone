# Aspire and OpenTelemetry Integration Summary

## Overview

This document summarizes the changes made to containerize the Roslyn-Stone MCP server using .NET Aspire and OpenTelemetry integration.

## Changes Made

### 1. New Projects

#### RoslynStone.ServiceDefaults
- **Purpose**: Provides OpenTelemetry configuration and service defaults
- **Location**: `src/RoslynStone.ServiceDefaults/`
- **Key Components**:
  - Extensions.cs: Configures OpenTelemetry logging, metrics, and tracing
  - Automatic OTLP exporter configuration
  - Health check endpoints
  - Service discovery support

#### RoslynStone.AppHost
- **Purpose**: Aspire orchestration host for local development
- **Location**: `src/RoslynStone.AppHost/`
- **Key Components**:
  - AppHost.cs: Configures MCP server as a containerized resource with HTTP transport
  - Provides Aspire dashboard for telemetry visualization
  - Manages service lifecycle and configuration
  - **MCP Inspector Integration**: Uses CommunityToolkit.Aspire.Hosting.McpInspector package to automatically launch and configure the inspector in development mode

#### RoslynStone.AppHost.Tests
- **Purpose**: Integration tests for Aspire configuration
- **Location**: `tests/RoslynStone.AppHost.Tests/`
- **Key Components**:
  - AppHostTests.cs: 7 tests verifying AppHost configuration
  - Tests verify HTTP transport, environment variables, resource setup, and inspector configuration
  - Fast-running tests that don't require Docker or DCP

### 2. Modified Projects

#### RoslynStone.Api
- **Changes**:
  - Added reference to ServiceDefaults project
  - Integrated `AddServiceDefaults()` in Program.cs
  - Supports both stdio and HTTP transport modes via `MCP_TRANSPORT` environment variable
  - HTTP transport mode provides REST endpoints for MCP protocol
  - Logging configured to stderr to avoid protocol interference

### 3. Transport Configuration

#### HTTP Transport (Default for Aspire)
- **Port**: Configurable via `MCP_HTTP_PORT` environment variable (default: 8080)
- **Endpoints**: `/mcp` for MCP HTTP protocol, `/health` for health checks
- **Usage**: Designed for container deployment and testing with tools like MCP Inspector
- **Security**: No authentication by default - configure before public exposure

#### Stdio Transport
- **Removed from AppHost**: Stdio transport configuration removed from Aspire AppHost
- **Still Available**: Can still be used by running the API directly with `MCP_TRANSPORT=stdio`
- **Use Case**: Direct execution, Claude Desktop integration, local development

### 3. Containerization

#### Dockerfile
- **Location**: `src/RoslynStone.Api/Dockerfile`
- **Features**:
  - Multi-stage build (build, publish, final)
  - Optimized layer caching
  - Based on .NET 10.0 SDK and runtime images
  - Minimal runtime dependencies
  - Configurable build configuration

#### .dockerignore
- **Purpose**: Optimize Docker build context
- **Excludes**: Build artifacts, tests, documentation, git files

#### docker-compose.yml
- **Purpose**: Local development with Aspire dashboard
- **Services**:
  - roslyn-stone-mcp: The MCP server container
  - aspire-dashboard: Telemetry visualization dashboard
- **Features**:
  - Automatic network configuration
  - OTLP endpoint configuration
  - Dashboard accessible at http://localhost:18888

### 4. CI/CD

#### .github/workflows/container.yml
- **Purpose**: Build and publish container images to GHCR
- **Triggers**: Push to main, tags, pull requests
- **Features**:
  - Multi-architecture builds (linux/amd64, linux/arm64)
  - Build caching with GitHub Actions cache
  - Automatic tagging strategy
  - Artifact attestation
  - Only pushes on non-PR events

### 5. Documentation

#### README.md
- **Updates**:
  - Added Aspire and OpenTelemetry to features list
  - Updated architecture diagram
  - Added Docker and Aspire usage instructions
  - New "Observability with OpenTelemetry" section
  - Container registry information
  - Updated technology stack
  - Docker Desktop configuration examples
  - Production deployment guidance

## OpenTelemetry Configuration

### Automatic Configuration
The ServiceDefaults project automatically configures:
- **Logging**: OpenTelemetry format with formatted messages and scopes
- **Metrics**: ASP.NET Core, HttpClient, and runtime instrumentation
- **Tracing**: Distributed tracing with activity sources

### OTLP Exporter
Configured via environment variables:
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OTLP endpoint URL
- `OTEL_SERVICE_NAME`: Service identifier
- `OTEL_RESOURCE_ATTRIBUTES`: Additional resource metadata

### Aspire Dashboard
- Provides real-time visualization during development
- Logs, metrics, and traces in a unified interface
- No configuration required for local development

## Usage Scenarios

### Local Development (Direct)
```bash
# Stdio transport (for Claude Desktop, etc.)
cd src/RoslynStone.Api
dotnet run

# HTTP transport
cd src/RoslynStone.Api
MCP_TRANSPORT=http dotnet run
```

### Local Development (Aspire)
```bash
cd src/RoslynStone.AppHost
dotnet run
# Aspire dashboard available at http://localhost:18888
# MCP server HTTP endpoint available on configured port (default: 8080)
# MCP Inspector UI automatically starts at http://localhost:6274
```

### Using MCP Inspector

The MCP Inspector is automatically launched when running the AppHost in development mode using the CommunityToolkit.Aspire.Hosting.McpInspector package.

**Automatic Configuration:**
- Inspector UI: http://localhost:6274 (configurable via `INSPECTOR_UI_PORT`)
- Inspector Proxy: http://localhost:6277 (configurable via `INSPECTOR_PROXY_PORT`)
- Automatically connects to the MCP server's HTTP endpoint

**Usage:**
1. Start the Aspire AppHost: `cd src/RoslynStone.AppHost && dotnet run`
2. Open the Inspector UI at http://localhost:6274
3. The inspector is automatically connected to your MCP server
4. Test MCP tools through the visual interface
5. Export server configurations for Claude Desktop or other MCP clients

**Technical Details:**
- Uses `AddMcpInspector()` extension from CommunityToolkit package
- Automatically discovers and connects to MCP resources via `WithMcpServer()`
- Inspector endpoints are named "client" (UI) and "server-proxy" (proxy)

### Docker Compose
```bash
docker-compose up --build
# Dashboard available at http://localhost:18888
# MCP server available via HTTP transport
```

### Production Container
```bash
docker pull ghcr.io/dylanlangston/roslyn-stone:latest
docker run -i --rm \
  -e OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317 \
  -e OTEL_SERVICE_NAME=roslyn-stone-mcp \
  ghcr.io/dylanlangston/roslyn-stone:latest
```

## Security Considerations

1. **No HTTP Endpoints**: The MCP server uses stdio transport only
2. **Minimal Runtime Image**: Based on aspnet:10.0 runtime (smaller attack surface)
3. **No Secrets in Container**: All configuration via environment variables
4. **Diagnostics Disabled**: `DOTNET_EnableDiagnostics=0` in production

## Testing

### Unit and Integration Tests
All existing tests continue to pass:
- 101 tests passing
- 1 test skipped (slow network operations)
- 0 failures
- Build succeeds without warnings

### Aspire Integration Tests
The `RoslynStone.AppHost.Tests` project provides integration tests for the Aspire configuration:
- 7 tests verifying AppHost setup and configuration
- Tests verify HTTP transport configuration
- Tests verify environment variables
- Tests verify resource endpoints
- Tests verify MCP Inspector integration
- Fast-running tests that don't require Docker or DCP

```bash
# Run Aspire tests
dotnet test tests/RoslynStone.AppHost.Tests

# Run all tests
dotnet test
```

### MCP Inspector Integration

The MCP Inspector is automatically launched by the AppHost in development mode using the CommunityToolkit.Aspire.Hosting.McpInspector package:

**Features:**
- Automatically starts when running `dotnet run` from AppHost in development
- Uses managed Aspire resource instead of manual npx execution
- Automatically discovers and connects to MCP server resources
- Provides interactive web UI at `http://localhost:6274`
- Test MCP tools without writing code
- View real-time request/response data
- Export server configurations for Claude Desktop and other clients

**Ports:**
- `6274`: Inspector UI (web interface) - endpoint name: "client"
- `6277`: MCP Proxy (protocol bridge) - endpoint name: "server-proxy"

**Environment Detection:**
The inspector only starts when:
- `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is set to `Development`
- Or when no environment is specified (defaults to development)

**Benefits:**
- Seamless testing experience alongside Aspire dashboard
- No manual setup or configuration required
- Automatic connection to MCP server via `WithMcpServer()` extension
- Visual feedback on tool execution
- Easy configuration export
- Better integration with Aspire lifecycle management

## Compatibility

- **Backward Compatible**: All existing functionality preserved
- **Stdio Transport**: MCP protocol communication unchanged
- **Configuration**: Existing environment variables still work
- **Dependencies**: Aspire packages are additive, no breaking changes

## Deployment Options

### Container Registries
- GitHub Container Registry (GHCR): `ghcr.io/dylanlangston/roslyn-stone`
- Can be pushed to any OCI-compatible registry

### Orchestration
- Docker Compose
- Kubernetes
- Azure Container Apps
- AWS ECS/Fargate
- Google Cloud Run

### Monitoring Solutions
- Azure Monitor / Application Insights
- AWS CloudWatch
- Google Cloud Operations
- Grafana / Prometheus / Jaeger
- Any OTLP-compatible collector

## Future Enhancements

Potential improvements for future iterations:
1. Kubernetes manifests with Helm charts
2. Health check endpoints (currently only in development)
3. Prometheus metrics endpoint
4. Additional instrumentation for REPL operations
5. Custom metrics for code execution performance
6. Trace sampling configuration
7. Log aggregation configuration

## Support

For issues or questions:
- GitHub Issues: https://github.com/dylanlangston/Roslyn-Stone/issues
- Documentation: README.md
- Aspire Docs: https://learn.microsoft.com/en-us/dotnet/aspire/
- OpenTelemetry Docs: https://opentelemetry.io/docs/languages/net/
