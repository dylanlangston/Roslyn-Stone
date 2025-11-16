namespace RoslynStone.Core.MCP;

/// <summary>
/// MCP tool definition
/// </summary>
public class McpTool
{
    /// <summary>
    /// Gets or sets the tool name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input schema for the tool
    /// </summary>
    public McpToolInputSchema InputSchema { get; set; } = new();
}

/// <summary>
/// MCP tool input schema
/// </summary>
public class McpToolInputSchema
{
    /// <summary>
    /// Gets or sets the schema type (always "object")
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// Gets or sets the properties of the input schema
    /// </summary>
    public Dictionary<string, McpToolProperty> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the required property names
    /// </summary>
    public List<string> Required { get; set; } = new();
}

/// <summary>
/// MCP tool property definition
/// </summary>
public class McpToolProperty
{
    /// <summary>
    /// Gets or sets the property type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property description
    /// </summary>
    public string? Description { get; set; }
}
