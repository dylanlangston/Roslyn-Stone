namespace RoslynStone.Core.MCP;

/// <summary>
/// MCP JSON-RPC request structure
/// </summary>
public class McpRequest
{
    /// <summary>
    /// Gets or sets the JSON-RPC version (always "2.0")
    /// </summary>
    public string Jsonrpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the request identifier
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the method name to invoke
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method parameters
    /// </summary>
    public object? Params { get; set; }
}

/// <summary>
/// MCP JSON-RPC response structure
/// </summary>
public class McpResponse
{
    /// <summary>
    /// Gets or sets the JSON-RPC version (always "2.0")
    /// </summary>
    public string Jsonrpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the response identifier matching the request
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the result of the method invocation
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets the error if the method invocation failed
    /// </summary>
    public McpError? Error { get; set; }
}

/// <summary>
/// MCP error structure
/// </summary>
public class McpError
{
    /// <summary>
    /// Gets or sets the error code
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error data
    /// </summary>
    public object? Data { get; set; }
}
