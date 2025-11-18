using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Resources;

/// <summary>
/// MCP resource for .NET documentation lookup
/// </summary>
[McpServerResourceType]
public class DocumentationResource
{
    /// <summary>
    /// Get XML documentation for a .NET type or method as a resource
    /// </summary>
    /// <param name="documentationService">The documentation service for symbol lookup</param>
    /// <param name="uri">The resource URI in format: doc://Fully.Qualified.TypeName</param>
    /// <returns>Documentation content in structured format</returns>
    [McpServerResource]
    [Description(
        "Access comprehensive XML documentation for .NET types, methods, properties, and other symbols. Returns detailed information including summary, remarks, parameters, return values, exceptions, and code examples. Use this to: understand .NET APIs, learn method signatures, discover available members, and get context-aware help. Supports all .NET 10 types and methods. URI format: doc://{FullyQualifiedTypeName}"
    )]
    public static DocumentationResponse GetDocumentation(
        DocumentationService documentationService,
        [Description(
            "Resource URI in the format 'doc://Fully.Qualified.TypeName'. Examples: 'doc://System.String', 'doc://System.Linq.Enumerable.Select', 'doc://System.Collections.Generic.List`1'"
        )]
            string uri
    )
    {
        // Extract symbol name from URI (remove "doc://" prefix)
        var symbolName = uri.StartsWith("doc://", StringComparison.OrdinalIgnoreCase)
            ? uri[6..]
            : uri;

        var doc = documentationService.GetDocumentation(symbolName);

        if (doc == null)
        {
            return new DocumentationResponse
            {
                Uri = uri,
                Found = false,
                MimeType = "application/json",
                Message =
                    $"Documentation not found for symbol: {symbolName}. Try well-known types like System.String, System.Linq.Enumerable, System.Collections.Generic.List`1, System.Threading.Tasks.Task, or System.Text.Json.JsonSerializer.",
            };
        }

        return new DocumentationResponse
        {
            Uri = uri,
            Found = true,
            MimeType = "application/json",
            SymbolName = doc.SymbolName,
            Summary = doc.Summary,
            Remarks = doc.Remarks,
            Parameters = doc.Parameters,
            Returns = doc.Returns,
            Exceptions = doc.Exceptions,
            Example = doc.Example,
        };
    }
}
