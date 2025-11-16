# Roslyn-Stone

A developer- and LLM-friendly C# REPL service built with Roslyn and ASP.NET Core, designed for Model Context Protocol (MCP) integration. Execute C# code, validate syntax, lookup documentation, and run single-file programs through a REST API.

## Features

✨ **C# REPL via Roslyn Scripting** - Execute C# code snippets with state preservation  
🔍 **Real-time Compile Error Reporting** - Get detailed compilation errors and warnings  
📚 **XML Documentation Lookup** - Query .NET type/method documentation via reflection  
📦 **NuGet Package Support** - Load external dependencies with `#r "nuget:Package"` (infrastructure ready)  
📄 **Single-File Execution** - Run standalone `.cs` files using `dotnet run-app`  
🏗️ **CQRS Architecture** - Clean separation of commands and queries without MediatR  
🔌 **MCP Protocol Integration** - JSON-RPC 2.0 compatible endpoints for LLM interactions  
🌐 **REST API** - Simple HTTP endpoints for all functionality

## Architecture

The solution follows clean architecture principles with CQRS pattern:

```
RoslynStone/
├── src/
│   ├── RoslynStone.Api/          # ASP.NET Core Web API
│   ├── RoslynStone.Core/         # Domain models, commands, queries, interfaces
│   └── RoslynStone.Infrastructure/ # Handlers, Roslyn services, implementations
└── tests/
    └── RoslynStone.Tests/        # xUnit tests
```

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
git clone https://github.com/dylanlangston/Rosyln-Stone.git
cd Rosyln-Stone

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API
cd src/RoslynStone.Api
dotnet run
```

The API will start on `http://localhost:5242` (or as configured in launchSettings.json).

## API Endpoints

### Root
```bash
GET /
# Returns service information and available endpoints
```

### REPL - Execute Code
```bash
POST /api/repl/execute
Content-Type: application/json

{
  "code": "Console.WriteLine(\"Hello, World!\"); return 42;"
}
```

Response:
```json
{
  "success": true,
  "returnValue": 42,
  "output": "Hello, World!\n",
  "errors": [],
  "warnings": [],
  "executionTime": "00:00:00.2228022"
}
```

### REPL - Validate Code
```bash
POST /api/repl/validate
Content-Type: application/json

{
  "code": "int x = \"not a number\";"
}
```

Response:
```json
[
  {
    "code": "CS0029",
    "message": "Cannot implicitly convert type 'string' to 'int'",
    "severity": "Error",
    "line": 1,
    "column": 9
  }
]
```

### Documentation Lookup
```bash
GET /api/documentation/{symbolName}
# Example: GET /api/documentation/System.String
```

### MCP Protocol Endpoint
```bash
POST /api/mcp
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/list"
}
```

Response:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "tools": [
      {
        "name": "execute_code",
        "description": "Execute C# code in the REPL and return the result",
        "inputSchema": { ... }
      },
      {
        "name": "validate_code",
        "description": "Validate C# code and return compilation errors/warnings",
        "inputSchema": { ... }
      },
      {
        "name": "get_documentation",
        "description": "Get XML documentation for a .NET type or method",
        "inputSchema": { ... }
      }
    ]
  }
}
```

## Examples

### Basic Expression Evaluation
```csharp
// Request: POST /api/repl/execute
{"code": "2 + 2"}

// Returns: 4
```

### Stateful Execution
```csharp
// First request
{"code": "int x = 10; x"}
// Returns: 10

// Second request (state preserved in same service instance)
{"code": "x + 5"}
// Returns: 15
```

### Console Output Capture
```csharp
{"code": "Console.WriteLine(\"Debug info\"); return \"Result\";"}

// Response includes both output and return value
{
  "output": "Debug info\n",
  "returnValue": "Result"
}
```

### Compilation Error Detection
```csharp
{"code": "string text = 123;"}

// Returns compilation error with line/column info
```

## Technology Stack

- **ASP.NET Core** - Web API framework
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
- **MCP**: Protocol models (McpRequest, McpResponse, McpTool)

### Infrastructure (Implementation Layer)
- **Services**: RoslynScriptingService, DocumentationService
- **CommandHandlers**: Execute commands and return results
- **QueryHandlers**: Fetch data without side effects

### API (Presentation Layer)
- **Controllers**: ReplController, DocumentationController, McpController
- **Configuration**: Dependency injection, CORS, routing

## Development

### Running Tests
```bash
dotnet test --logger "console;verbosity=normal"
```

### Adding New Commands/Queries

1. Define command/query in `Core/Commands` or `Core/Queries`
2. Implement handler in `Infrastructure/CommandHandlers` or `Infrastructure/QueryHandlers`
3. Register handler in `Program.cs` DI container
4. Add controller endpoint in `Api/Controllers`

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
