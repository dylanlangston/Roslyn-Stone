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

**C# REPL via Roslyn Scripting** - Execute C# code snippets with state preservation  
**Real-time Compile Error Reporting** - Get detailed compilation errors and warnings  
**XML Documentation Lookup** - Query .NET type/method documentation via reflection  
**NuGet Package Support** - Search, discover, and load NuGet packages dynamically  
**MCP Protocol** - Official ModelContextProtocol SDK with stdio and HTTP transports  
**Dual Transport** - Support for both stdio (local) and HTTP (remote) MCP connections  
**AI-Friendly** - Designed for LLM interactions via Model Context Protocol  
**Built-in Guidance** - Comprehensive prompts help LLMs use the REPL effectively  
**Rich Tool Descriptions** - Detailed, LLM-friendly descriptions with examples and context  
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

## What Can It Do?

Once configured, your AI assistant can use these tools naturally. For example:

- "Execute this C# code: `var numbers = Enumerable.Range(1, 10); numbers.Sum()`"
- "Search for JSON parsing packages in NuGet"
- "Load the Newtonsoft.Json package"
- "Show me the documentation for System.Linq.Enumerable"

The AI assistant will automatically call the appropriate MCP tools to execute your requests.

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
