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
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Documentation content in structured format</returns>
    [McpServerResource]
    [Description(
        "Access comprehensive XML documentation for .NET types, methods, properties, and other symbols, including NuGet packages. Returns detailed information including summary, remarks, parameters, return values, exceptions, and code examples. Use this to: understand .NET APIs, learn method signatures, discover available members, and get context-aware help. Supports all .NET 10 types and methods, plus NuGet packages. URI formats: 'doc://{FullyQualifiedTypeName}' for .NET types, or 'doc://{PackageId}/{FullyQualifiedTypeName}' for NuGet package types"
    )]
    public static async Task<DocumentationResponse> GetDocumentation(
        DocumentationService documentationService,
        [Description(
            "Resource URI in the format 'doc://Fully.Qualified.TypeName' for .NET types, or 'doc://PackageId/Fully.Qualified.TypeName' for NuGet package types. Examples: 'doc://System.String', 'doc://System.Linq.Enumerable.Select', 'doc://Newtonsoft.Json/Newtonsoft.Json.JsonConvert'"
        )]
            string uri,
        CancellationToken cancellationToken = default
    )
    {
        // Extract symbol name and optional package ID from URI
        // Format: doc://[PackageId/]Fully.Qualified.TypeName
        var uriContent = uri.StartsWith("doc://", StringComparison.OrdinalIgnoreCase)
            ? uri[6..]
            : uri;

        string? packageId = null;
        string symbolName;

        // Check if URI contains a package ID (has a slash and first part looks like a package name)
        var firstSlash = uriContent.IndexOf('/');
        if (firstSlash > 0)
        {
            var potentialPackageId = uriContent[..firstSlash];
            // Simple heuristic: if the first part doesn't contain dots or looks like a namespace,
            // treat it as a package ID. Package names typically have dots but fewer than namespaces.
            // Example: "Newtonsoft.Json" vs "System.Collections.Generic"
            if (
                !potentialPackageId.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                && !potentialPackageId.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
            )
            {
                packageId = potentialPackageId;
                symbolName = uriContent[(firstSlash + 1)..];
            }
            else
            {
                symbolName = uriContent;
            }
        }
        else
        {
            symbolName = uriContent;
        }

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
                    : $"Documentation not found for symbol: {symbolName}. Try well-known types like System.String, System.Linq.Enumerable, System.Collections.Generic.List`1, System.Threading.Tasks.Task, or System.Text.Json.JsonSerializer. For NuGet packages, use format: doc://PackageId/TypeName (e.g., doc://Newtonsoft.Json/Newtonsoft.Json.JsonConvert).";

            return new DocumentationResponse
            {
                Uri = uri,
                Found = false,
                MimeType = "application/json",
                Message = exampleMessage,
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
