# Getting Started with Roslyn-Stone Development

This guide covers advanced setup options, development workflows, and technical details for contributors and developers working on Roslyn-Stone.

For basic usage, see the main [README.md](README.md).

## Table of Contents

- [Local Development Setup](#local-development-setup)
- [Transport Modes](#transport-modes)
- [Development Environments](#development-environments)
- [Advanced Configuration](#advanced-configuration)
- [Development Tools](#development-tools)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Adding New MCP Tools](#adding-new-mcp-tools)
- [Testing and Debugging](#testing-and-debugging)

## Local Development Setup

### Prerequisites

- .NET 10.0 SDK or later
- C# 13
- Docker (for containerized deployment)
- VS Code with Dev Containers extension (optional)
- .NET Aspire workload (optional, for local orchestration)

### Building from Source

```bash
# Clone the repository
git clone https://github.com/dylanlangston/Roslyn-Stone.git
cd Roslyn-Stone

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the MCP server (stdio transport - default)
cd src/RoslynStone.Api
dotnet run
```

## Transport Modes

Roslyn-Stone supports two MCP transport modes:

### Stdio Transport (Default)

- Best for local, single-machine integrations
- Used by Claude Desktop and other local MCP clients
- Communication via stdin/stdout
- No network ports required

### HTTP Transport

- Best for remote access and web-based integrations
- Accessible over the network via HTTP/SSE
- Endpoint at `/mcp` (e.g., `http://localhost:8080/mcp`)
- ⚠️ **WARNING**: Do not expose publicly without authentication, authorization, and network restrictions. This server can execute arbitrary C# code.

To switch transport modes, set the `MCP_TRANSPORT` environment variable:

```bash
# Stdio (default)
MCP_TRANSPORT=stdio dotnet run

# HTTP
MCP_TRANSPORT=http dotnet run --urls "http://localhost:8080"
```

## Development Environments

### With Aspire (Orchestrated)

```bash
# Install Aspire workload
dotnet workload install aspire

# Run with Aspire dashboard for observability
cd src/RoslynStone.AppHost
dotnet run
```

This will start:
- **Aspire Dashboard** at `http://localhost:18888` - View logs, metrics, and traces
- **MCP Inspector UI** at `http://localhost:6274` - Interactive tool testing interface (development mode only)
- **MCP Proxy** at `http://localhost:6277` - Protocol bridge for the inspector
- **MCP Server (stdio)** - Stdio transport instance for local testing
- **MCP Server (HTTP)** at `http://localhost:8080/mcp` - HTTP transport instance for remote access

The MCP Inspector is automatically started in development mode, providing a web-based interface to test and debug MCP tools in real-time.

### Development Container

The repository includes a fully configured devcontainer with Docker-in-Docker support for isolated development:

```bash
# Open the repo in VS Code
code .

# Press F1 and select "Dev Containers: Reopen in Container"
# The container will automatically build, restore dependencies, and build the project
```

See [`.devcontainer/README.md`](.devcontainer/README.md) for more details about the devcontainer setup.

### Docker Compose

```bash
# Build and run both stdio and HTTP variants with Docker Compose
docker-compose up --build

# Run only the stdio variant
docker-compose up roslyn-stone-mcp-stdio

# Run only the HTTP variant
docker-compose up roslyn-stone-mcp-http

# Access Aspire dashboard at http://localhost:18888
# Access HTTP MCP endpoint at http://localhost:8080/mcp
```

The server supports both stdio and HTTP transport modes:
- **Stdio transport**: Reads JSON-RPC messages from stdin and writes responses to stdout, with logging to stderr
- **HTTP transport**: Exposes MCP endpoints via HTTP/SSE for remote access at `/mcp`

## Advanced Configuration

### Custom MCP Server Configurations

For local development without Docker:

**Claude Desktop:**
```json
{
  "mcpServers": {
    "roslyn-stone": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/Roslyn-Stone/src/RoslynStone.Api"],
      "env": {
        "DOTNET_ENVIRONMENT": "Development",
        "MCP_TRANSPORT": "stdio"
      }
    }
  }
}
```

### HTTP Transport Configuration

For remote MCP servers or web-based integrations, use HTTP transport:

**Local HTTP Server:**
```bash
# Start the server with HTTP transport
cd src/RoslynStone.Api
MCP_TRANSPORT=http ASPNETCORE_URLS=http://localhost:8080 dotnet run
```

**Docker HTTP Server:**
```bash
# Run with HTTP transport
docker run -e MCP_TRANSPORT=http -e ASPNETCORE_URLS=http://+:8080 -p 8080:8080 ghcr.io/dylanlangston/roslyn-stone:latest
```

**MCP Client Configuration (HTTP):**
```json
{
  "mcpServers": {
    "roslyn-stone-http": {
      "type": "http",
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

The HTTP endpoint supports:
- **Server-Sent Events (SSE)** for streaming responses
- **CORS** support for web-based clients - ⚠️ Configure strict origin allowlists in production. Never use wildcard (*) CORS for code execution endpoints.
- **Standard MCP protocol** over HTTP

See `.github/MCP_CONFIGURATION.md` for more configuration examples.

### OpenTelemetry Configuration

Configure OpenTelemetry export with environment variables:

```bash
# OTLP endpoint (default: Aspire dashboard)
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:18889

# Service name
OTEL_SERVICE_NAME=roslyn-stone-mcp

# Additional resource attributes
OTEL_RESOURCE_ATTRIBUTES=service.namespace=roslyn-stone,deployment.environment=production
```

### Production Deployment

For production, configure the OTLP endpoint to send telemetry to your monitoring solution:

- Azure Monitor / Application Insights
- AWS CloudWatch
- Google Cloud Operations
- Grafana / Prometheus / Jaeger
- Any OTLP-compatible collector

## Development Tools

This project uses several development tools:

- **CSharpier** - Code formatter (`dotnet csharpier <file-or-directory>`)
- **ReSharper CLI** - Code analysis (`jb <command>`)
- **Cake** - Build automation (`dotnet cake <script>`)

See `.github/COPILOT_ENVIRONMENT.md` for details on the custom environment setup.

## Testing

### Running Tests

```bash
dotnet test --logger "console;verbosity=normal"

# Run tests by category
dotnet test --filter "Category=Unit"

# Run tests by component
dotnet test --filter "Component=REPL"
```

### Test Coverage

The project maintains high test coverage with automated reporting in CI:

- **Line Coverage**: >86% (target: 80%)
- **Branch Coverage**: >62% (target: 75%)
- **Total Tests**: 103+ tests

#### Run Tests with Coverage

```bash
# Using Cake build script (recommended)
dotnet cake --target=Test-Coverage

# Using dotnet CLI
dotnet test --collect:"XPlat Code Coverage"
```

Coverage reports are generated in `./artifacts/coverage/` with detailed metrics including:
- Line coverage percentage
- Branch coverage percentage
- Per-file coverage analysis
- Uncovered code locations

#### Generate HTML Coverage Report

```bash
dotnet cake --target=Test-Coverage-Report
```

Opens a detailed HTML report at `./artifacts/coverage-report/index.html` with:
- Interactive file browser
- Line-by-line coverage visualization
- Coverage badges
- Historical trends

### Benchmarks

Performance benchmarks using [BenchmarkDotNet](https://benchmarkdotnet.org/) to track and optimize critical operations:

#### Available Benchmarks

- **RoslynScriptingService** - REPL execution performance
  - Simple expressions
  - Variable assignments
  - LINQ queries
  - Complex operations
- **CompilationService** - Code compilation performance
  - Simple class compilation
  - Complex code compilation
  - Error handling
- **NuGetService** - Package operations performance
  - Package search
  - Version lookup
  - README retrieval

#### Run Benchmarks

```bash
# Run all benchmarks (Release configuration)
dotnet cake --target=Benchmark

# Run specific benchmark
dotnet run --project tests/RoslynStone.Benchmarks --configuration Release -- --filter *RoslynScriptingService*
```

Results are saved to `./artifacts/benchmarks/` with:
- Execution times (Min, Max, Mean, Median)
- Memory allocations
- Statistical analysis

See [tests/RoslynStone.Benchmarks/README.md](tests/RoslynStone.Benchmarks/README.md) for detailed documentation.

### Load Tests

Load tests validate the server can handle concurrent requests at scale:

#### Test Configuration

- **Concurrency**: 300 concurrent requests per round
- **Rounds**: 10 rounds per scenario
- **Scenarios**: Expression evaluation, LINQ queries, NuGet search
- **Total Requests**: 12,000 (300 × 10 × 4 scenarios)

#### Prerequisites

Start the server in HTTP mode:

```bash
cd src/RoslynStone.Api
MCP_TRANSPORT=http dotnet run
```

#### Run Load Tests

```bash
# Using Cake build script
dotnet cake --target=Load-Test

# Using dotnet CLI with custom configuration
dotnet run --project tests/RoslynStone.LoadTests -- http://localhost:7071 300 10
```

Arguments:
1. Base URL (default: `http://localhost:7071`)
2. Concurrency (default: 300)
3. Rounds (default: 10)

#### Expected Results

A healthy server should achieve:
- ✅ Success rate > 99%
- ✅ Average response time < 100ms
- ✅ Throughput > 1000 req/sec

See [tests/RoslynStone.LoadTests/README.md](tests/RoslynStone.LoadTests/README.md) for detailed documentation.

### Continuous Integration

The CI pipeline runs on every push and pull request:

```bash
# Run full CI pipeline locally
dotnet cake --target=CI
```

CI includes:
1. ✅ Code formatting check (CSharpier)
2. ✅ Code quality analysis (ReSharper)
3. ✅ Build verification
4. ✅ Test execution with coverage
5. ✅ Coverage threshold validation

Artifacts generated:
- Test results (`.trx` files)
- Coverage reports (Cobertura XML)
- ReSharper inspection reports
- Build logs

## Project Structure

### Core (Domain Layer)
- **Models**: ExecutionResult, DocumentationInfo, CompilationError, PackageReference, PackageMetadata, PackageVersion, PackageSearchResult
- **Domain Types**: Simple records and classes representing domain concepts

### Infrastructure (Implementation Layer)
- **Tools**: MCP tool implementations (ReplTools, NuGetTools) - Active operations
- **Resources**: MCP resource implementations (DocumentationResource, NuGetSearchResource, NuGetPackageResource, ReplStateResource) - Passive data access
- **Services**: RoslynScriptingService, DocumentationService, NuGetService, CompilationService, AssemblyExecutionService, ReplContextManager
- **Functional Helpers**: Pure functions for diagnostics, transformations, and utilities

### Api (Presentation Layer)
- **Program.cs**: Host configuration with MCP server setup
- **Stdio Transport**: JSON-RPC communication via stdin/stdout
- **HTTP Transport**: HTTP/SSE communication for remote access
- **Logging**: Configured to stderr to avoid protocol interference

## Adding New MCP Tools

1. Create tool method in `Infrastructure/Tools` with `[McpServerTool]` attribute
2. Add to existing `[McpServerToolType]` class or create new one
3. Use dependency injection for services (RoslynScriptingService, IReplContextManager, etc.)
4. Include comprehensive XML documentation with `<param>` and `<returns>` tags
5. Add `[Description]` attributes for MCP protocol metadata
6. Tools are auto-discovered via `WithToolsFromAssembly()`

Example:
```csharp
[McpServerToolType]
public class MyTools
{
    /// <summary>
    /// Execute custom operation
    /// </summary>
    /// <param name="service">Injected service</param>
    /// <param name="contextManager">Context manager for stateful operations</param>
    /// <param name="input">Input parameter</param>
    /// <param name="contextId">Optional context ID for session continuity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result description</returns>
    [McpServerTool]
    [Description("Tool description for MCP")]
    public static async Task<object> MyTool(
        MyService service,
        IReplContextManager contextManager,
        [Description("Input description")] string input,
        [Description("Optional context ID from previous execution")] string? contextId = null,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

## Adding New MCP Resources

1. Create resource class in `Infrastructure/Resources` implementing base resource patterns
2. Add `[McpServerResourceType]` attribute to the class
3. Implement URI parsing logic for your resource patterns
4. Return structured data (not operations)
5. Resources are auto-discovered via `WithResourcesFromAssembly()`

Example:
```csharp
[McpServerResourceType]
public class MyDataResource
{
    /// <summary>
    /// Provides access to my data
    /// </summary>
    [McpServerResource("mydata://{id}")]
    [Description("Access my data by ID")]
    public static async Task<object> GetMyData(
        [Description("Data ID")] string id,
        MyDataService service,
        CancellationToken cancellationToken = default)
    {
        var data = await service.GetDataAsync(id, cancellationToken);
        return new { id, data };
    }
}
```

## Testing and Debugging

### MCP Inspector

The [MCP Inspector](https://github.com/modelcontextprotocol/inspector) is an interactive developer tool for testing and debugging MCP servers. It provides a web-based UI to explore available tools, test requests, and view responses in real-time.

#### Integrated with Aspire (Recommended)

When running with Aspire, the MCP Inspector is automatically started in development mode:

```bash
cd src/RoslynStone.AppHost
dotnet run
```

The inspector will be available at:
- **Inspector UI**: `http://localhost:6274` - Interactive web interface
- **Aspire Dashboard**: `http://localhost:18888` - Observability and logs

This provides a seamless development experience with both testing and observability in one place.

#### Standalone Inspector

To inspect the server without Aspire, use npx to run the inspector directly:

```bash
# From the repository root, run the compiled server through the inspector
npx @modelcontextprotocol/inspector dotnet run --project src/RoslynStone.Api/RoslynStone.Api.csproj
```

The inspector will start two services:
- **MCP Inspector UI** at `http://localhost:6274` - Interactive web interface
- **MCP Proxy** at `http://localhost:6277` - Protocol bridge

#### Using the Inspector

1. Open `http://localhost:6274` in your browser
2. The server connection is automatically established
3. Explore available tools in the left sidebar
4. Test tools by clicking them and providing parameters
5. View responses, including return values and output
6. Export server configuration for Claude Desktop or other clients

#### Inspecting with Environment Variables

Pass environment variables to configure the server:

```bash
npx @modelcontextprotocol/inspector \
  -e DOTNET_ENVIRONMENT=Development \
  -e OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 \
  dotnet run --project src/RoslynStone.Api/RoslynStone.Api.csproj
```

#### Inspecting the Container

You can also inspect the containerized version:

```bash
# Inspect the container image
npx @modelcontextprotocol/inspector docker run -i --rm ghcr.io/dylanlangston/roslyn-stone:latest
```

#### Custom Ports

If you need to use different ports:

```bash
CLIENT_PORT=8080 SERVER_PORT=9000 npx @modelcontextprotocol/inspector \
  dotnet run --project src/RoslynStone.Api/RoslynStone.Api.csproj
```

#### Exporting Configuration

The inspector provides buttons to export server configurations:

- **Server Entry**: Copies the launch configuration to clipboard for use in `mcp.json`
- Compatible with Claude Desktop, Cursor, Claude Code, and other MCP clients

Example exported configuration:
```json
{
  "command": "dotnet",
  "args": ["run", "--project", "/path/to/Roslyn-Stone/src/RoslynStone.Api/RoslynStone.Api.csproj"],
  "env": {
    "DOTNET_ENVIRONMENT": "Development"
  }
}
```

For more details, see the [MCP Inspector documentation](https://modelcontextprotocol.io/docs/tools/inspector).

## Additional Resources

- [Model Context Protocol (MCP)](https://github.com/modelcontextprotocol/specification)
- [Roslyn Scripting APIs](https://learn.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting)
- [Dynamic Compilation Best Practices](DYNAMIC_COMPILATION_BEST_PRACTICES.md)
- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/languages/net/)
