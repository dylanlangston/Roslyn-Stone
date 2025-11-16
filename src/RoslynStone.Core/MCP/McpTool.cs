namespace RoslynStone.Core.MCP;

/// <summary>
/// MCP tool definition
/// </summary>
public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public McpToolInputSchema InputSchema { get; set; } = new();
}

/// <summary>
/// MCP tool input schema
/// </summary>
public class McpToolInputSchema
{
    public string Type { get; set; } = "object";
    public Dictionary<string, McpToolProperty> Properties { get; set; } = new();
    public List<string> Required { get; set; } = new();
}

/// <summary>
/// MCP tool property definition
/// </summary>
public class McpToolProperty
{
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}
