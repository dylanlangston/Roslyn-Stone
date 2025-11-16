using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP tools for .NET documentation lookup
/// </summary>
[McpServerToolType]
public class DocumentationTools
{
    /// <summary>
    /// Get XML documentation for a .NET type or method
    /// </summary>
    [McpServerTool]
    [Description("Get XML documentation for a .NET type, method, property, or other symbol including summary, remarks, parameters, and examples")]
    public static object GetDocumentation(
        DocumentationService documentationService,
        [Description("The fully qualified name of the type or method (e.g., 'System.String', 'System.Linq.Enumerable.Select')")] string symbolName)
    {
        var doc = documentationService.GetDocumentation(symbolName);
        
        if (doc == null)
        {
            return new
            {
                found = false,
                message = $"Documentation not found for symbol: {symbolName}"
            };
        }
        
        return new
        {
            found = true,
            symbolName = doc.SymbolName,
            summary = doc.Summary,
            remarks = doc.Remarks,
            parameters = doc.Parameters,
            returns = doc.Returns,
            exceptions = doc.Exceptions,
            example = doc.Example
        };
    }
}
