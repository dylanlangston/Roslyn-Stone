# Roslyn-Stone

> **Note**: This project was collaboratively built with GitHub Copilot, embracing the future of AI-assisted development.

A developer- and LLM-friendly C# REPL (Read-Eval-Print Loop) service that brings the power of the Roslyn compiler to AI coding assistants. Named as a playful nod to the Rosetta Stone—the ancient artifact that helped decode languages—Roslyn-Stone helps AI systems decode and execute C# code seamlessly through the Model Context Protocol (MCP).

Execute C# code, validate syntax, load NuGet packages, and lookup documentation through MCP for seamless integration with Claude Code, VS Code, and other AI-powered development tools.

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

**C# REPL via Roslyn Scripting** - Execute C# code with optional stateful sessions  
**Context Management** - Maintain variables and state across executions with contextId  
**Real-time Compile Error Reporting** - Get detailed compilation errors and warnings  
**Resources & Tools** - Proper MCP separation: Resources (data) vs Tools (operations)  
**Documentation Access** - Query .NET type/method docs via `doc://` resource URIs  
**NuGet Integration** - Search packages via `nuget://` resources, load with tools  
**MCP Protocol** - Official ModelContextProtocol SDK with stdio and HTTP transports  
**Dual Transport** - Support for both stdio (local) and HTTP (remote) MCP connections  
**AI-Friendly** - Designed for LLM interactions via Model Context Protocol  
**Token-Optimized Prompts** - 59% smaller prompts using resource references  
**Containerized** - Docker support with .NET Aspire orchestration  
**OpenTelemetry** - Built-in observability with logs, metrics, and traces  

## Architecture

Roslyn-Stone implements the Model Context Protocol (MCP) following best practices by properly distinguishing between:

- **Resources** (passive data sources): URI-based read-only access to documentation (`doc://`), NuGet packages (`nuget://`), and REPL state (`repl://`)
- **Tools** (active operations): Context-aware C# execution, validation, and package loading with optional session management
- **Prompts** (optimized templates): Token-efficient guidance for LLMs with resource references

The solution follows clean architecture principles with functional programming patterns. It implements best practices for dynamic code compilation and execution, including proper AssemblyLoadContext usage for memory management.

### Key Components

- **Context Management**: Thread-safe session lifecycle with automatic cleanup (30min timeout)
- **Stateful REPL**: Variables and types persist within sessions via optional `contextId` parameter
- **Resource Discovery**: Query docs, packages, and state before execution for efficient workflows
- **Token Optimization**: Prompts reduced 59% (4500→1850 tokens) by referencing resources

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

- **EvaluateCsharp** - Execute C# code in a REPL session
  - Optional `contextId` parameter for stateful sessions
  - Returns `contextId` for session continuity
- **ValidateCsharp** - Validate C# syntax and semantics
  - Optional `contextId` for context-aware validation
- **ResetRepl** - Reset REPL sessions
  - Optional `contextId` to reset specific session or all sessions
- **LoadNuGetPackage** - Load NuGet packages into REPL environment
  - Packages persist in session until reset

### MCP Prompts

Roslyn-Stone includes built-in prompts to help LLMs use the REPL effectively:

- **GetStartedWithCsharpRepl** - Comprehensive introduction to Roslyn-Stone's capabilities, quick start guide, and best practices
- **CodeExperimentationWorkflow** - Step-by-step guide for iterative development using the REPL
- **PackageIntegrationGuide** - How to discover, evaluate, and use NuGet packages
- **DebuggingAndErrorHandling** - Understanding compilation errors, runtime errors, and debugging techniques

These prompts provide detailed guidance on how to use the REPL, including examples, common patterns, and tips for success.

## What Can It Do?

Once configured, your AI assistant can interact with the REPL naturally:

**Execute code with state preservation:**
```
User: "Create a variable x = 10"
Assistant: [Calls EvaluateCsharp] → Returns contextId: "abc-123"

User: "Multiply x by 2"
Assistant: [Calls EvaluateCsharp with contextId: "abc-123"] → Returns 20
```

**Query documentation:**
```
User: "Show me the documentation for System.Linq.Enumerable"
Assistant: [Reads doc://System.Linq.Enumerable resource]

User: "What methods are available in Newtonsoft.Json.JsonConvert?"
Assistant: [Reads doc://Newtonsoft.Json@Newtonsoft.Json.JsonConvert resource]
```

**Search and load packages:**
```
User: "Search for JSON parsing packages"
Assistant: [Reads nuget://search?q=json resource]

User: "Load Newtonsoft.Json"
Assistant: [Calls LoadNuGetPackage tool]
```

**Check REPL state:**
```
User: "How many REPL sessions are active?"
Assistant: [Reads repl://sessions resource]
```

The AI assistant automatically uses Resources for queries and Tools for operations.

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
