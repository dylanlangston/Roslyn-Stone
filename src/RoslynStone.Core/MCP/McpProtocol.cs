namespace RoslynStone.Core.MCP;

/// <summary>
/// MCP JSON-RPC request structure
/// </summary>
public class McpRequest
{
    public string Jsonrpc { get; set; } = "2.0";
    public string? Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public object? Params { get; set; }
}

/// <summary>
/// MCP JSON-RPC response structure
/// </summary>
public class McpResponse
{
    public string Jsonrpc { get; set; } = "2.0";
    public string? Id { get; set; }
    public object? Result { get; set; }
    public McpError? Error { get; set; }
}

/// <summary>
/// MCP error structure
/// </summary>
public class McpError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}
