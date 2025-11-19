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
    /// <param name="symbolName">The fully qualified type or method name</param>
    /// <param name="packageId">Optional NuGet package ID for package-specific types</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Documentation content in structured format</returns>
    [McpServerTool]
    [Description(
        "Access comprehensive XML documentation for .NET types, methods, properties, and other symbols, including NuGet packages. Returns detailed information including summary, remarks, parameters, return values, exceptions, and code examples. Use this to: understand .NET APIs, learn method signatures, discover available members, and get context-aware help. Supports all .NET 10 types and methods, plus NuGet packages."
    )]
    public static async Task<object> GetDocumentation(
        DocumentationService documentationService,
        [Description(
            "Fully qualified type or method name. Examples: 'System.String', 'System.Linq.Enumerable.Select', 'System.Collections.Generic.List`1'"
        )]
            string symbolName,
        [Description(
            "Optional NuGet package ID for package-specific types. Example: 'Newtonsoft.Json' for 'Newtonsoft.Json.JsonConvert'"
        )]
            string? packageId = null,
        CancellationToken cancellationToken = default
    )
    {
        var doc = await documentationService.GetDocumentationAsync(
            symbolName,
            packageId,
            cancellationToken
        );

        if (doc == null)
        {
            var exampleMessage =
                packageId != null
                    ? $"Documentation not found for symbol: {symbolName} in package: {packageId}. Make sure the package exists and contains XML documentation."
                    : $"Documentation not found for symbol: {symbolName}. Try well-known types like System.String, System.Linq.Enumerable, System.Collections.Generic.List`1, System.Threading.Tasks.Task, or System.Text.Json.JsonSerializer. For NuGet packages, provide packageId parameter (e.g., packageId: 'Newtonsoft.Json').";

            return new
            {
                found = false,
                message = exampleMessage,
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
