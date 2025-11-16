# Roslyn-Stone

A developer- and LLM-friendly C# REPL service built with Roslyn and the Model Context Protocol (MCP) SDK. Execute C# code, validate syntax, and lookup documentation through MCP stdio transport for seamless AI integration.

## Features

✨ **C# REPL via Roslyn Scripting** - Execute C# code snippets with state preservation  
🔍 **Real-time Compile Error Reporting** - Get detailed compilation errors and warnings  
📚 **XML Documentation Lookup** - Query .NET type/method documentation via reflection  
📦 **NuGet Package Support** - Load external dependencies (infrastructure ready)  
🏗️ **CQRS Architecture** - Clean separation of commands and queries  
🔌 **MCP Protocol** - Official ModelContextProtocol SDK with stdio transport  
🤖 **AI-Friendly** - Designed for LLM interactions via Model Context Protocol  

## Architecture

The solution follows clean architecture principles with CQRS pattern and MCP integration. It implements best practices for dynamic code compilation and execution, including proper AssemblyLoadContext usage for memory management. See `DYNAMIC_COMPILATION_BEST_PRACTICES.md` for details.

```
RoslynStone/
├── src/
│   ├── RoslynStone.Api/            # Console Host with MCP Server
│   ├── RoslynStone.Core/           # Domain models, commands, queries, interfaces
│   └── RoslynStone.Infrastructure/ # MCP Tools, Roslyn services, handlers
└── tests/
    └── RoslynStone.Tests/          # xUnit tests
```

### MCP Tools

- **EvaluateCsharp** - Execute C# code with return value and output
- **ValidateCsharp** - Syntax/semantic validation without execution
- **GetDocumentation** - XML documentation lookup for .NET symbols
- **ResetRepl** - Clear REPL state

### CQRS Pattern

- **Commands**: Operations that change state (ExecuteCode, LoadPackage, ExecuteFile)
- **Queries**: Read-only operations (GetDocumentation, ValidateCode)
- **Handlers**: Implement business logic for commands and queries
- **No MediatR**: Direct dependency injection for simplicity and transparency

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- C# 13

### Build and Run

```bash
# Clone the repository
git clone https://github.com/dylanlangston/Roslyn-Stone.git
cd Roslyn-Stone

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the MCP server (stdio transport)
cd src/RoslynStone.Api
dotnet run
```

The server uses stdio transport for MCP protocol communication. It reads JSON-RPC messages from stdin and writes responses to stdout, with logging to stderr.

## Usage with MCP Clients

### Claude Desktop Configuration

Add to your Claude Desktop config (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

```json
{
  "mcpServers": {
    "roslyn-stone": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/Roslyn-Stone/src/RoslynStone.Api"],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

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

## Technology Stack

- **Model Context Protocol SDK** - MCP stdio transport
- **Microsoft.Extensions.Hosting** - Host builder with DI
- **Roslyn** - C# compiler and scripting APIs
  - `Microsoft.CodeAnalysis.CSharp.Scripting` - Script execution
  - `Microsoft.CodeAnalysis.CSharp.Workspaces` - Code analysis
- **xUnit** - Testing framework
- **System.Reflection** - XML documentation lookup

## Project Structure

### Core (Domain Layer)
- **CQRS**: Interfaces for commands, queries, and handlers
- **Commands**: ExecuteCodeCommand, LoadPackageCommand, ExecuteFileCommand
- **Queries**: GetDocumentationQuery, ValidateCodeQuery
- **Models**: ExecutionResult, DocumentationInfo, CompilationError, PackageReference

### Infrastructure (Implementation Layer)
- **Tools**: MCP tool implementations (ReplTools, DocumentationTools)
- **Services**: RoslynScriptingService, DocumentationService
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

## Security Considerations

⚠️ **Important**: This is a code execution service. Deploy with appropriate security measures:

- Run in isolated containers/sandboxes
- Implement rate limiting
- Add authentication/authorization
- Restrict network access
- Monitor resource usage
- Use read-only file systems where possible

## Future Enhancements

- [ ] Full NuGet package resolution and loading
- [ ] Persistent REPL sessions with user isolation
- [ ] Code snippet history and caching
- [ ] Syntax highlighting and IntelliSense data
- [ ] Performance metrics and profiling
- [ ] WebSocket support for interactive sessions
- [ ] Docker container support
- [ ] OpenTelemetry integration

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
