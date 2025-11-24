---
title: Roslyn Stone
emoji: 🪨
colorFrom: gray
colorTo: blue
sdk: docker
app_port: 7860
pinned: false
tags:
  - mcp
  - csharp
  - roslyn
  - repl
  - building-mcp-track-enterprise
---

# Roslyn-Stone

> **Note**: This project was collaboratively built with GitHub Copilot, embracing the future of AI-assisted development.

A developer- and LLM-friendly C# sandbox for creating single-file utility programs through the Model Context Protocol (MCP). Named as a playful nod to the Rosetta Stone—the ancient artifact that helped decode languages—Roslyn-Stone helps AI systems create runnable C# programs using file-based apps (top-level statements).

Build complete, runnable .cs files using the power of the Roslyn compiler. Execute C# code, validate syntax, load NuGet packages, and lookup documentation through MCP for seamless integration with Claude Code, VS Code, and other AI-powered development tools.

Perfect for creating command-line utilities, data processing scripts, automation tools, and quick C# programs without project scaffolding.

## Quick Start with Docker

The fastest way to get started is using the pre-built Docker container with your favorite MCP-enabled editor:

### Claude Code / VS Code / Cursor

Add this to your MCP configuration file:

**Linux/macOS**: `~/.config/Code/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json`  
**Windows**: `%APPDATA%\Code\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`

```json
{
   "mcpServers": {
       "roslyn-stone": {
           "command": "docker",
           "args": [
               "run",
               "-i",
               "--rm",
               "-e", "DOTNET_USE_POLLING_FILE_WATCHER=1",
               "ghcr.io/dylanlangston/roslyn-stone:latest"
           ]
       }
   }
}
```

**Using Podman?** Just replace `"docker"` with `"podman"` in the command field.

### Claude Desktop

Add to `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or equivalent:

```json
{
   "mcpServers": {
       "roslyn-stone": {
           "command": "docker",
           "args": [
               "run",
               "-i",
               "--rm",
               "-e", "DOTNET_USE_POLLING_FILE_WATCHER=1",
               "ghcr.io/dylanlangston/roslyn-stone:latest"
           ]
       }
   }
}
```

That's it! The container provides isolated execution of C# code with minimal setup and enhanced security.

## Features

**File-Based C# Apps** - Create single-file utilities using top-level statements (no class/Main boilerplate)  
**C# Execution via Roslyn** - Execute and test C# code with full .NET 10 support  
**Iterative Development** - Build programs incrementally with optional stateful sessions  
**Real-time Error Feedback** - Get detailed compilation errors and warnings  
**Resources & Tools** - Proper MCP separation: Resources (data) vs Tools (operations)  
**Documentation Access** - Query .NET type/method docs via `doc://` resource URIs  
**NuGet Integration** - Search packages via `nuget://` resources, load with tools  
**MCP Protocol** - Official ModelContextProtocol SDK with stdio and HTTP transports  
**Dual Transport** - Support for both stdio (local) and HTTP (remote) MCP connections  
**AI-Friendly** - Designed for LLM interactions via Model Context Protocol  
**Token-Optimized Prompts** - Efficient guidance for creating utility programs  
**Containerized** - Docker support with .NET Aspire orchestration  
**OpenTelemetry** - Built-in observability with logs, metrics, and traces  

## Architecture

Roslyn-Stone implements the Model Context Protocol (MCP) to help LLMs create single-file C# utility programs. It follows best practices by properly distinguishing between:

- **Resources** (passive data sources): URI-based read-only access to documentation (`doc://`), NuGet packages (`nuget://`), and execution state (`repl://`)
- **Tools** (active operations): Code execution, validation, and package loading for building utility programs
- **Prompts** (optimized templates): Token-efficient guidance for creating file-based C# apps

The solution follows clean architecture principles with functional programming patterns. It implements best practices for dynamic code compilation and execution, including proper AssemblyLoadContext usage for memory management.

### Key Components

- **Context Management**: Thread-safe session lifecycle with automatic cleanup (30min timeout)
- **Stateful Execution**: Variables and types persist within sessions for iterative development
- **Resource Discovery**: Query docs, packages, and state before execution for efficient workflows
- **Token Optimization**: Prompts guide LLMs to create complete, runnable .cs files

See `MCP_ARCHITECTURE.md` for detailed design documentation and `DYNAMIC_COMPILATION_BEST_PRACTICES.md` for compilation details.

```
RoslynStone/
├── src/
│   ├── RoslynStone.Api/            # Console Host with MCP Server
│   ├── RoslynStone.Core/           # Domain models (ExecutionResult, PackageMetadata, etc.)
│   ├── RoslynStone.Infrastructure/ # MCP Tools, Roslyn services, functional helpers
│   ├── RoslynStone.ServiceDefaults/# OpenTelemetry and Aspire defaults
│   └── RoslynStone.AppHost/        # Aspire orchestration
└── tests/
    ├── RoslynStone.Tests/          # xUnit unit and integration tests
    ├── RoslynStone.Benchmarks/     # BenchmarkDotNet performance tests
    └── RoslynStone.LoadTests/      # HTTP load and concurrency tests
```

### Architecture Principles

- **Functional Programming**: Leverages LINQ, pure functions, and functional composition
- **Direct Service Calls**: MCP Tools call services directly without abstraction layers
- **Thread Safety**: Static and instance-level synchronization for reliable parallel execution
- **Immutable Models**: Uses records and readonly properties where appropriate

### MCP Resources (Read-Only Data Access)

Resources provide URI-based access to passive data sources:

- **`doc://{symbolName}`** - .NET XML documentation lookup
  - Example: `doc://System.String`, `doc://System.Linq.Enumerable.Select`
  - **New:** Supports NuGet packages: `doc://{packageId}@{symbolName}`
  - Example: `doc://Newtonsoft.Json@Newtonsoft.Json.JsonConvert`
- **`nuget://search?q={query}`** - Search NuGet packages
  - Example: `nuget://search?q=json&take=10`
- **`nuget://packages/{id}/versions`** - Get package version list
  - Example: `nuget://packages/Newtonsoft.Json/versions`
- **`nuget://packages/{id}/readme`** - Get package README
  - Example: `nuget://packages/Newtonsoft.Json/readme?version=13.0.3`
- **`repl://state`** - General REPL information and capabilities
- **`repl://sessions`** - List active REPL sessions
- **`repl://sessions/{contextId}/state`** - Session-specific metadata

### MCP Tools (Active Operations)

Tools perform operations and can modify state. All tools support optional context management:

**Execution Tools:**
- **EvaluateCsharp** - Execute C# code to create and test single-file utility programs
  - Optional `contextId` parameter for iterative development
  - Returns `contextId` for session continuity
- **ValidateCsharp** - Validate C# syntax and semantics before execution
  - Optional `contextId` for context-aware validation
- **ResetRepl** - Reset execution sessions
  - Optional `contextId` to reset specific session or all sessions
- **GetReplInfo** - Get execution environment information and capabilities
  - Optional `contextId` for session-specific state
  - Returns framework version, capabilities, tips, and examples

**NuGet Tools:**
- **LoadNuGetPackage** - Load NuGet packages into REPL environment
  - Packages persist in session until reset
- **SearchNuGetPackages** - Search for NuGet packages
  - Parameters: `query`, `skip`, `take` for pagination
  - Alternative to `nuget://search` resource for clients without resource support
- **GetNuGetPackageVersions** - Get all versions of a package
  - Parameter: `packageId`
  - Alternative to `nuget://packages/{id}/versions` resource
- **GetNuGetPackageReadme** - Get package README content
  - Parameters: `packageId`, optional `version`
  - Alternative to `nuget://packages/{id}/readme` resource

**Documentation Tools:**
- **GetDocumentation** - Get XML documentation for .NET types/methods
  - Parameters: `symbolName`, optional `packageId`
  - Alternative to `doc://` resource for clients without resource support
  - Example: GetDocumentation("System.String") or GetDocumentation("JsonConvert", "Newtonsoft.Json")

> **Note:** Both Resources and Tools are provided for maximum client compatibility. Resources are preferred for passive data access, while Tools work in all MCP clients.

### MCP Prompts

Roslyn-Stone includes built-in prompts to help LLMs create single-file C# utility programs:

- **GetStartedWithCsharpRepl** - Comprehensive introduction to file-based C# apps, development workflow, and best practices
- **ReplBestPractices** - Patterns for creating single-file utilities with complete examples
- **WorkingWithPackages** - How to discover, evaluate, and use NuGet packages in utility programs
- **PackageIntegrationGuide** - Deep dive into package integration with detailed utility examples

These prompts provide detailed guidance on creating runnable .cs files, including examples, common patterns, and tips for success.

## What Can It Do?

Once configured, your AI assistant can help you create single-file C# utility programs:

**Build a simple utility:**
```
User: "Create a utility that lists files in the current directory"
Assistant: [Calls EvaluateCsharp with file-based app code]
→ Returns complete .cs file using top-level statements
```

**Iterative development:**
```
User: "Create a JSON formatter utility"
Assistant: 
  1. [Reads nuget://search?q=json to find Newtonsoft.Json]
  2. [Calls LoadNuGetPackage("Newtonsoft.Json")]
  3. [Calls EvaluateCsharp with complete utility code]
→ Returns json-formatter.cs ready to run with `dotnet run json-formatter.cs`
```

**Query documentation:**
```
User: "Show me how to use System.IO.File.ReadAllText"
Assistant: [Option 1: Reads doc://System.IO.File.ReadAllText resource]
         [Option 2: Calls GetDocumentation("System.IO.File.ReadAllText")]
```

**Validate before execution:**
```
User: "Check if this C# code is valid: <code>"
Assistant: [Calls ValidateCsharp with code]
→ Returns syntax validation results
```

The AI assistant creates complete, runnable single-file C# programs using top-level statements—no class or Main method boilerplate needed.

## For Developers

Want to contribute, run from source, or learn more about the internals? Check out our comprehensive [Getting Started Guide](GETTING_STARTED.md) for:

- Building from source
- Development environment setup
- Running tests and benchmarks
- Adding new MCP tools
- Debugging with MCP Inspector
- Architecture details

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
- [x] Docker container support
- [x] OpenTelemetry integration
- [ ] Persistent REPL sessions with user isolation
- [ ] Code snippet history and caching
- [ ] Syntax highlighting and IntelliSense data
- [ ] WebSocket support for interactive sessions
- [ ] Multi-architecture container images (amd64, arm64)

## Contributing

Contributions are welcome! Please see our [Getting Started Guide](GETTING_STARTED.md) for development setup instructions. Feel free to submit issues and pull requests.

## License

See [LICENSE](LICENSE) file for details.

## Learn More

- [Getting Started Guide](GETTING_STARTED.md) - Development and contribution guide
- [Model Context Protocol (MCP)](https://github.com/modelcontextprotocol/specification)
- [Roslyn Scripting APIs](https://learn.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting)
- [Dynamic Compilation Best Practices](DYNAMIC_COMPILATION_BEST_PRACTICES.md)
