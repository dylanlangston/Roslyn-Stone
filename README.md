# Roslyn-Stone

A developer- and LLM-friendly C# REPL service built with Roslyn and the Model Context Protocol (MCP) SDK. Execute C# code, validate syntax, and lookup documentation through MCP stdio or HTTP transport for seamless AI integration.

## Features

✨ **C# REPL via Roslyn Scripting** - Execute C# code snippets with state preservation  
🔍 **Real-time Compile Error Reporting** - Get detailed compilation errors and warnings  
📚 **XML Documentation Lookup** - Query .NET type/method documentation via reflection  
📦 **NuGet Package Support** - Search, discover, and load NuGet packages dynamically  
🏗️ **CQRS Architecture** - Clean separation of commands and queries  
🔌 **MCP Protocol** - Official ModelContextProtocol SDK with stdio and HTTP transports  
🌐 **Dual Transport** - Support for both stdio (local) and HTTP (remote) MCP connections  
🤖 **AI-Friendly** - Designed for LLM interactions via Model Context Protocol  
💡 **Built-in Guidance** - Comprehensive prompts help LLMs use the REPL effectively  
📖 **Rich Tool Descriptions** - Detailed, LLM-friendly descriptions with examples and context  
🐳 **Containerized** - Docker support with .NET Aspire orchestration  
📊 **OpenTelemetry** - Built-in observability with logs, metrics, and traces  

## Architecture

The solution follows clean architecture principles with CQRS pattern and MCP integration. It implements best practices for dynamic code compilation and execution, including proper AssemblyLoadContext usage for memory management. See `DYNAMIC_COMPILATION_BEST_PRACTICES.md` for details.

```
RoslynStone/
├── src/
│   ├── RoslynStone.Api/            # Console Host with MCP Server
│   ├── RoslynStone.Core/           # Domain models, commands, queries, interfaces
│   ├── RoslynStone.Infrastructure/ # MCP Tools, Roslyn services, handlers
│   ├── RoslynStone.ServiceDefaults/# OpenTelemetry and Aspire defaults
│   └── RoslynStone.AppHost/        # Aspire orchestration
└── tests/
    └── RoslynStone.Tests/          # xUnit tests
```

### MCP Tools

#### REPL Tools
- **EvaluateCsharp** - Execute C# code with return value and output
- **ValidateCsharp** - Syntax/semantic validation without execution
- **ResetRepl** - Clear REPL state
- **GetReplInfo** - Get information about the REPL environment and capabilities

#### Documentation Tools
- **GetDocumentation** - XML documentation lookup for .NET symbols

#### NuGet Tools
- **SearchNuGetPackages** - Search for NuGet packages by name, description, or tags
- **GetNuGetPackageVersions** - Get all available versions of a package
- **GetNuGetPackageReadme** - Get the README content for a package
- **LoadNuGetPackage** - Load a NuGet package into the REPL environment

### MCP Prompts

Roslyn-Stone includes built-in prompts to help LLMs use the REPL effectively:

- **GetStartedWithCsharpRepl** - Comprehensive introduction to Roslyn-Stone's capabilities, quick start guide, and best practices
- **CodeExperimentationWorkflow** - Step-by-step guide for iterative development using the REPL
- **PackageIntegrationGuide** - How to discover, evaluate, and use NuGet packages
- **DebuggingAndErrorHandling** - Understanding compilation errors, runtime errors, and debugging techniques

These prompts provide detailed guidance on how to use the REPL, including examples, common patterns, and tips for success.

### CQRS Pattern

- **Commands**: Operations that change state (ExecuteCode, LoadPackage, ExecuteFile)
- **Queries**: Read-only operations (GetDocumentation, ValidateCode, SearchPackages, GetPackageVersions, GetPackageReadme)
- **Handlers**: Implement business logic for commands and queries
- **No MediatR**: Direct dependency injection for simplicity and transparency

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- C# 13
- Docker (optional, for containerized deployment)
- VS Code with Dev Containers extension (optional, for containerized development)
- .NET Aspire workload (optional, for local orchestration)

### Build and Run

#### Local Development

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

# Run the MCP server with HTTP transport
cd src/RoslynStone.Api
MCP_TRANSPORT=http dotnet run --urls "http://localhost:8080"
```

#### Transport Modes

Roslyn-Stone supports two MCP transport modes:

**Stdio Transport (Default)**
- Best for local, single-machine integrations
- Used by Claude Desktop and other local MCP clients
- Communication via stdin/stdout
- No network ports required

**HTTP Transport**
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

#### With Aspire (Orchestrated)

```bash
# Install Aspire workload (or skip if you don't need local orchestration)
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

#### Development Container

The repository includes a fully configured devcontainer with Docker-in-Docker support for isolated development:

```bash
# Open the repo in VS Code
code .

# Press F1 and select "Dev Containers: Reopen in Container"
# The container will automatically build, restore dependencies, and build the project
```

See [`.devcontainer/README.md`](.devcontainer/README.md) for more details about the devcontainer setup and Docker-in-Docker testing.

#### Docker Compose (Containerized)

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

## Container Registry

Pre-built container images are available from GitHub Container Registry:

```bash
docker pull ghcr.io/dylanlangston/roslyn-stone:latest
```

## Usage with MCP Clients

### Claude Desktop Configuration (Stdio - Local)

Add to your Claude Desktop config (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

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

### Claude Desktop Configuration (Stdio - Docker)

```json
{
  "mcpServers": {
    "roslyn-stone": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "ghcr.io/dylanlangston/roslyn-stone:latest"],
      "env": {
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

## MCP Tools

### EvaluateCsharp

Execute C# code in the REPL:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "EvaluateCsharp",
    "arguments": {
      "code": "var x = 10; x + 5"
    }
  },
  "id": 1
}
```

Response includes success status, return value, output, errors, warnings, and execution time.

### ValidateCsharp

Validate C# code without executing:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "ValidateCsharp",
    "arguments": {
      "code": "int x = \"not a number\";"
    }
  },
  "id": 2
}
```

Returns validation results with isValid flag and list of issues (errors/warnings).

### GetDocumentation

Lookup XML documentation for .NET symbols:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "GetDocumentation",
    "arguments": {
      "symbolName": "System.String"
    }
  },
  "id": 3
}
```

Returns documentation including summary, remarks, parameters, returns, exceptions, and examples.

### ResetRepl

Clear the REPL state:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "ResetRepl"
  },
  "id": 4
}
```

### SearchNuGetPackages

Search for NuGet packages:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "SearchNuGetPackages",
    "arguments": {
      "query": "json",
      "skip": 0,
      "take": 5
    }
  },
  "id": 5
}
```

Returns package metadata including ID, title, description, authors, latest version, download count, and URLs.

### GetNuGetPackageVersions

Get all available versions of a package:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "GetNuGetPackageVersions",
    "arguments": {
      "packageId": "Newtonsoft.Json"
    }
  },
  "id": 6
}
```

Returns a list of versions with metadata including prerelease and deprecated flags.

### GetNuGetPackageReadme

Get the README content for a package:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "GetNuGetPackageReadme",
    "arguments": {
      "packageId": "Newtonsoft.Json",
      "version": "13.0.3"
    }
  },
  "id": 7
}
```

Returns the README content if available.

### LoadNuGetPackage

Load a NuGet package into the REPL:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "LoadNuGetPackage",
    "arguments": {
      "packageName": "Newtonsoft.Json",
      "version": "13.0.3"
    }
  },
  "id": 8
}
```

After loading, the package's types and methods are available in the REPL.

## Development Tools

This project uses a custom GitHub Copilot environment with pre-installed development tools:

- **CSharpier** - Code formatter (`dotnet csharpier <file-or-directory>`)
- **ReSharper CLI** - Code analysis (`jb <command>`)
- **Cake** - Build automation (`dotnet cake <script>`)

See `.github/COPILOT_ENVIRONMENT.md` for details on the custom environment setup.

## Examples

### Basic Expression Evaluation
```csharp
// Execute simple expression
code: "2 + 2"
// Returns: 4
```

### Stateful Execution
```csharp
// First execution
code: "int x = 10; x"
// Returns: 10

// Second execution (state preserved in same REPL instance)
code: "x + 5"
// Returns: 15
```

### Console Output Capture
```csharp
code: "Console.WriteLine(\"Debug info\"); return \"Result\";"

// Response includes both output and return value
{
  "output": "Debug info\n",
  "returnValue": "Result"
}
```

### Compilation Error Detection
```csharp
code: "string text = 123;"

// Returns compilation error with line/column info and error code
```

### Using NuGet Packages
```csharp
// First, load the package
LoadNuGetPackage: { packageName: "Newtonsoft.Json", version: "13.0.3" }

// Then use it in the REPL
code: "using Newtonsoft.Json; var obj = new { Name = \"Test\" }; JsonConvert.SerializeObject(obj)"

// Returns: "{\"Name\":\"Test\"}"
```

### Getting Started with Prompts
```json
{
  "jsonrpc": "2.0",
  "method": "prompts/get",
  "params": {
    "name": "GetStartedWithCsharpRepl"
  },
  "id": 1
}
```

This returns a comprehensive guide covering:
- Core capabilities of Roslyn-Stone
- Quick start examples
- Best practices for using the REPL
- Common patterns and workflows
- Tips for success

Use the prompts to help LLMs understand how to effectively use Roslyn-Stone for C# development.

### Checking REPL Environment
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "GetReplInfo"
  },
  "id": 1
}
```

Returns information about the REPL including framework version, available namespaces, capabilities, tips, and examples.
```

## Technology Stack

- **Model Context Protocol SDK** - MCP stdio transport
- **Microsoft.Extensions.Hosting** - Host builder with DI
- **.NET Aspire** - Cloud-native orchestration and observability
  - `Aspire.Hosting` - Application orchestration
  - Service Defaults - OpenTelemetry configuration
- **OpenTelemetry** - Distributed tracing, metrics, and logging
  - OTLP Exporter - Send telemetry to Aspire dashboard or other collectors
  - ASP.NET Core Instrumentation - HTTP and runtime metrics
- **Roslyn** - C# compiler and scripting APIs
  - `Microsoft.CodeAnalysis.CSharp.Scripting` - Script execution
  - `Microsoft.CodeAnalysis.CSharp.Workspaces` - Code analysis
- **NuGet.Protocol** - NuGet package discovery and downloading
- **xUnit** - Testing framework
- **System.Reflection** - XML documentation lookup
- **Docker** - Containerization and deployment

## Observability with OpenTelemetry

RoslynStone includes built-in OpenTelemetry support for comprehensive observability:

### Telemetry Features

- **Logs**: Structured logging with OpenTelemetry format
- **Metrics**: Runtime metrics, HTTP client metrics, custom application metrics
- **Traces**: Distributed tracing for request correlation

### Aspire Dashboard

When running with Aspire (`dotnet run` from AppHost), the dashboard provides:

- Real-time log viewing with structured data
- Metrics visualization (P50, P90, P99 percentiles)
- Distributed trace visualization
- Resource health monitoring

Access the dashboard at `http://localhost:18888` when running with Aspire.

### Environment Variables

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

## Project Structure

### Core (Domain Layer)
- **CQRS**: Interfaces for commands, queries, and handlers
- **Commands**: ExecuteCodeCommand, LoadPackageCommand, ExecuteFileCommand
- **Queries**: GetDocumentationQuery, ValidateCodeQuery, SearchPackagesQuery, GetPackageVersionsQuery, GetPackageReadmeQuery
- **Models**: ExecutionResult, DocumentationInfo, CompilationError, PackageReference, PackageMetadata, PackageVersion, PackageSearchResult
### Infrastructure (Implementation Layer)
- **Tools**: MCP tool implementations (ReplTools, DocumentationTools, NuGetTools)
- **Services**: RoslynScriptingService, DocumentationService, NuGetService
- **CommandHandlers**: Execute commands and return results
- **QueryHandlers**: Fetch data without side effects

### Api (Presentation Layer)
- **Program.cs**: Host configuration with MCP server setup
- **Stdio Transport**: JSON-RPC communication via stdin/stdout
- **Logging**: Configured to stderr to avoid protocol interference

## Development

### Running Tests
```bash
dotnet test --logger "console;verbosity=normal"

# Run tests by category
dotnet test --filter "Category=Unit"

# Run tests by component
dotnet test --filter "Component=REPL"
```

### Adding New MCP Tools

1. Create tool method in `Infrastructure/Tools` with `[McpServerTool]` attribute
2. Add to existing `[McpServerToolType]` class or create new one
3. Use dependency injection for services (RoslynScriptingService, etc.)
4. Include comprehensive XML documentation with `<param>` and `<returns>` tags
5. Add `[Description]` attributes for MCP protocol metadata
6. Tools are auto-discovered via `WithToolsFromAssembly()`

Example:
```csharp
[McpServerToolType]
public class MyTools
{
    /// <summary>
    /// Tool description
    /// </summary>
    /// <param name="service">Injected service</param>
    /// <param name="input">Input parameter</param>
    /// <returns>Result description</returns>
    [McpServerTool]
    [Description("Tool description for MCP")]
    public static async Task<object> MyTool(
        MyService service,
        [Description("Input description")] string input,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### Testing and Debugging with MCP Inspector

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

## Security Considerations

⚠️ **Important**: This is a code execution service. Deploy with appropriate security measures:

- Run in isolated containers/sandboxes
- Implement rate limiting
- Add authentication/authorization
- Restrict network access
- Monitor resource usage
- Use read-only file systems where possible

## Future Enhancements

- [x] Full NuGet package resolution and loading
- [x] Docker container support (including devcontainer with Docker-in-Docker)
- [x] OpenTelemetry integration
- [ ] Persistent REPL sessions with user isolation
- [ ] Code snippet history and caching
- [ ] Syntax highlighting and IntelliSense data
- [ ] Performance metrics and profiling
- [ ] WebSocket support for interactive sessions
- [ ] Multi-architecture container images (amd64, arm64)

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

See [LICENSE](LICENSE) file for details.

## References

- [Model Context Protocol (MCP)](https://github.com/modelcontextprotocol/specification)
- [Roslyn Scripting APIs](https://learn.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting)
- [CQRS Pattern without MediatR](https://cezarypiatek.github.io/post/why-i-dont-use-mediatr-for-cqrs/)
- [dotnet run-app](https://devblogs.microsoft.com/dotnet/announcing-dotnet-run-app/)
- [Dynamic Compilation Best Practices](DYNAMIC_COMPILATION_BEST_PRACTICES.md) - Based on Laurent Kempé's approach
