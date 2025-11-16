---
scope:
  languages:
    - csharp
  patterns:
    - "**/*mcp*"
    - "**/*MCP*"
    - "**/*protocol*"
    - "**/*server*"
---

# Model Context Protocol (MCP) Instructions

## MCP Server Development

When working on MCP server implementation:

### Required Components
1. **Tool Registration**: All tools must be registered with the MCP server
2. **JSON-RPC 2.0 Compliance**: Follow the JSON-RPC 2.0 specification
3. **Schema Definitions**: Provide clear JSON schemas for all tool inputs/outputs
4. **Discovery Support**: Implement tool discovery endpoints

### Code Patterns

#### Tool Definition
```csharp
[McpTool("tool_name", "Clear description of what this tool does")]
public async Task<ToolResult> ToolNameAsync(ToolParameters parameters)
{
    // Validate inputs
    ValidateParameters(parameters);
    
    // Execute operation
    var result = await ExecuteOperationAsync(parameters);
    
    // Return structured result
    return new ToolResult
    {
        Content = result,
        IsError = false
    };
}
```

#### Error Handling
```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    return new ToolResult
    {
        Content = $"Error: {ex.Message}\nContext: {context}\nSuggestion: {suggestion}",
        IsError = true
    };
}
```

### Transport Layers
- Use `WithStdioServerTransport()` for console-based servers
- Use `WithHttpServerTransport()` for HTTP-based servers
- Ensure proper connection lifecycle management

### Security
- Validate all inputs before processing
- Sanitize file paths to prevent directory traversal
- Implement rate limiting for expensive operations
- Use authentication/authorization where appropriate

### Performance
- Implement caching for expensive operations
- Use incremental analysis where possible
- Avoid blocking operations on the main thread
- Consider memory usage with large codebases

## Testing MCP Servers

### Integration Testing
- Test tool discovery
- Test tool execution with valid inputs
- Test error handling with invalid inputs
- Test concurrent operations
- Test connection management

### Client Testing
- Test with Claude Desktop configuration
- Test with GitHub Copilot in agent mode
- Verify tool discoverability
- Validate response formats

## Configuration

MCP servers should support configuration via:
- Command-line arguments
- Environment variables
- Configuration files (JSON/YAML)

### Example Configuration
```json
{
  "server": {
    "name": "roslyn-stone-mcp",
    "version": "1.0.0"
  },
  "transport": "stdio",
  "features": {
    "repl": true,
    "analysis": true,
    "nuget": true
  }
}
```

## Documentation

Document each MCP tool with:
- Clear purpose and use cases
- Parameter descriptions and types
- Return value format
- Error conditions
- Usage examples
- Performance considerations

## Best Practices

1. **Idempotency**: Make operations idempotent where possible
2. **Atomicity**: Ensure operations are atomic or provide rollback
3. **Observability**: Log important operations and errors
4. **Versioning**: Include version information in tool metadata
5. **Backward Compatibility**: Maintain compatibility with older clients when possible
