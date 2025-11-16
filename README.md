# Roslyn-Stone

A developer- and LLM-friendly C# REPL service built with Roslyn and the Model Context Protocol (MCP) SDK. Execute C# code, validate syntax, and lookup documentation through MCP stdio transport for seamless AI integration.

## Features

✨ **C# REPL via Roslyn Scripting** - Execute C# code snippets with state preservation  
🔍 **Real-time Compile Error Reporting** - Get detailed compilation errors and warnings  
📚 **XML Documentation Lookup** - Query .NET type/method documentation via reflection  
📦 **NuGet Package Support** - Search, discover, and load NuGet packages dynamically  
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

#### REPL Tools
- **EvaluateCsharp** - Execute C# code with return value and output
- **ValidateCsharp** - Syntax/semantic validation without execution
- **ResetRepl** - Clear REPL state

#### Documentation Tools
- **GetDocumentation** - XML documentation lookup for .NET symbols

#### NuGet Tools
- **SearchNuGetPackages** - Search for NuGet packages by name, description, or tags
- **GetNuGetPackageVersions** - Get all available versions of a package
- **GetNuGetPackageReadme** - Get the README content for a package
- **LoadNuGetPackage** - Load a NuGet package into the REPL environment

### CQRS Pattern

- **Commands**: Operations that change state (ExecuteCode, LoadPackage, ExecuteFile)
- **Queries**: Read-only operations (GetDocumentation, ValidateCode, SearchPackages, GetPackageVersions, GetPackageReadme)
- **Handlers**: Implement business logic for commands and queries
- **No MediatR**: Direct dependency injection for simplicity and transparency

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- C# 13
- Optional: Docker and VS Code with Dev Containers extension for containerized development

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

# Run the MCP server (stdio transport)
cd src/RoslynStone.Api
dotnet run
```

#### Development Container

The repository includes a fully configured devcontainer with Docker-in-Docker support for isolated development:

```bash
# Clone the repository
git clone https://github.com/dylanlangston/Roslyn-Stone.git
cd Roslyn-Stone

# Open in VS Code
code .

# Press F1 and select "Dev Containers: Reopen in Container"
# The container will automatically build, restore dependencies, and build the project
```

See [`.devcontainer/README.md`](.devcontainer/README.md) for more details about the devcontainer setup and Docker-in-Docker testing.

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

## Technology Stack

- **Model Context Protocol SDK** - MCP stdio transport
- **Microsoft.Extensions.Hosting** - Host builder with DI
- **Roslyn** - C# compiler and scripting APIs
  - `Microsoft.CodeAnalysis.CSharp.Scripting` - Script execution
  - `Microsoft.CodeAnalysis.CSharp.Workspaces` - Code analysis
- **NuGet.Protocol** - NuGet package discovery and downloading
- **xUnit** - Testing framework
- **System.Reflection** - XML documentation lookup

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
- [x] Docker container support (devcontainer with Docker-in-Docker)
- [ ] Persistent REPL sessions with user isolation
- [ ] Code snippet history and caching
- [ ] Syntax highlighting and IntelliSense data
- [ ] Performance metrics and profiling
- [ ] WebSocket support for interactive sessions
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
