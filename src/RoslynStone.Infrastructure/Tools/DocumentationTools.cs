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
    /// <param name="documentationService">The documentation service for symbol lookup</param>
    /// <param name="symbolName">The fully qualified name of the type or method (e.g., 'System.String', 'System.Linq.Enumerable.Select')</param>
    /// <returns>An object containing documentation information including summary, remarks, parameters, returns, exceptions, and examples</returns>
    [McpServerTool]
    [Description(
        "Retrieve comprehensive XML documentation for any .NET type, method, property, or other symbol. Returns detailed information including summary, detailed remarks, parameter descriptions, return value info, exception documentation, and code examples. Use this to: understand how .NET APIs work, learn method signatures and parameters, discover available members, see usage examples, and get context-aware help while coding. Supports all .NET 10 types and methods."
    )]
    public static object GetDocumentation(
        DocumentationService documentationService,
        [Description(
            "The fully qualified name of the .NET symbol to look up. Examples: 'System.String' (for the String class), 'System.Linq.Enumerable.Select' (for LINQ Select method), 'System.Collections.Generic.List`1' (for generic List), 'System.DateTime.Now' (for properties). Use namespaces and type names exactly as they appear in C# code."
        )]
            string symbolName
    )
    {
        var doc = documentationService.GetDocumentation(symbolName);

        if (doc == null)
        {
            return new
            {
                found = false,
                message = $"Documentation not found for symbol: {symbolName}",
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
            example = doc.Example,
        };
    }
}
