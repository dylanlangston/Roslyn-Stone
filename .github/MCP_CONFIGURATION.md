# MCP Server Configuration Examples

This document provides example configurations for integrating the Roslyn-Stone MCP server with various AI tools.

## Claude Desktop Configuration

To use Roslyn-Stone with Claude Desktop, add the following to your Claude Desktop configuration file:

### Windows
Location: `%APPDATA%\Claude\claude_desktop_config.json`

### macOS
Location: `~/Library/Application Support/Claude/claude_desktop_config.json`

### Linux
Location: `~/.config/Claude/claude_desktop_config.json`

### Configuration
```json
{
  "mcpServers": {
    "roslyn-stone": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/Roslyn-Stone/Roslyn-Stone.csproj"],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## GitHub Copilot Agent Mode

Roslyn-Stone can be used with GitHub Copilot in agent mode. The custom agent configuration is already set up in `.github/agents/CSharpExpert.agent.md`.

To enable MCP tools for the agent:
1. Ensure the MCP server is running
2. Configure the agent to use stdio transport
3. Reference the server in agent configuration

## VS Code Configuration

For VS Code integration, you can configure the MCP server in your workspace settings:

### `.vscode/settings.json`
```json
{
  "mcp.servers": {
    "roslyn-stone": {
      "command": "dotnet",
      "args": ["run", "--project", "${workspaceFolder}/Roslyn-Stone.csproj"],
      "transport": "stdio"
    }
  }
}
```

## Environment Variables

The following environment variables can be used to configure Roslyn-Stone:

- `ROSLYN_STONE_PORT`: Port for HTTP transport (default: stdio)
- `ROSLYN_STONE_LOG_LEVEL`: Logging level (Debug, Info, Warning, Error)
- `ROSLYN_STONE_CACHE_DIR`: Directory for caching compiled scripts
- `ROSLYN_STONE_MAX_MEMORY`: Maximum memory allocation in MB
- `ROSLYN_STONE_TIMEOUT`: Script execution timeout in seconds

## Transport Options

### Standard I/O (stdio)
Default transport for console applications:
```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport();
```

### HTTP
For web-based integrations:
```csharp
builder.Services
    .AddMcpServer()
    .WithHttpServerTransport(options =>
    {
        options.Port = 8080;
    });
```

## Security Configuration

For production deployments:
```json
{
  "mcpServers": {
    "roslyn-stone": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/Roslyn-Stone/Roslyn-Stone.csproj"],
      "env": {
        "DOTNET_ENVIRONMENT": "Production",
        "ROSLYN_STONE_SANDBOX": "true",
        "ROSLYN_STONE_RESTRICT_IO": "true",
        "ROSLYN_STONE_RESTRICT_NETWORK": "true"
      }
    }
  }
}
```

## Tool Discovery

To verify the server is working and discover available tools:

### Using stdio transport
```bash
echo '{"jsonrpc":"2.0","method":"tools/list","id":1}' | dotnet run --project Roslyn-Stone.csproj
```

### Using HTTP transport
```bash
curl -X POST http://localhost:8080/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

## Troubleshooting

### Server Not Starting
- Check that .NET 8.0 SDK or later is installed: `dotnet --version`
- Verify the project builds: `dotnet build`
- Check logs for error messages

### Tools Not Discovered
- Verify the MCP server is running
- Check transport configuration (stdio vs HTTP)
- Ensure tool methods have proper `[McpTool]` attributes

### Connection Issues
- For stdio: Ensure no buffering issues in your terminal
- For HTTP: Check firewall settings and port availability
- Verify JSON-RPC 2.0 message format

## Example Usage

### Execute C# Code
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "evaluate_csharp",
    "arguments": {
      "code": "var x = 42; return x * 2;"
    }
  },
  "id": 1
}
```

### Add NuGet Package
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "add_package",
    "arguments": {
      "packageId": "Newtonsoft.Json",
      "version": "13.0.3"
    }
  },
  "id": 2
}
```

## Further Reading

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [MCP C# SDK Documentation](https://github.com/modelcontextprotocol/csharp-sdk)
- [Claude Desktop MCP Guide](https://docs.anthropic.com/claude/docs/model-context-protocol)
