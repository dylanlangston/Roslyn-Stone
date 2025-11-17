using System.ComponentModel;
using System.Text.Encodings.Web;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Resources;

/// <summary>
/// MCP resource for NuGet package information
/// </summary>
[McpServerResourceType]
public class NuGetPackageResource
{
    /// <summary>
    /// Get available versions for a NuGet package as a resource
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="uri">The resource URI in format: nuget://packages/PackageId/versions</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>List of package versions with metadata</returns>
    [McpServerResource]
    [Description(
        "Access all published versions of a specific NuGet package, sorted from newest to oldest. Returns version numbers, download counts, and flags indicating prerelease or deprecated status. Use this to choose a specific version to load, check version history, avoid deprecated versions, and identify stable releases."
    )]
    public static async Task<object> GetPackageVersions(
        NuGetService nugetService,
        [Description(
            "Resource URI in the format 'nuget://packages/PackageId/versions'. Examples: 'nuget://packages/Newtonsoft.Json/versions', 'nuget://packages/Microsoft.Extensions.Logging/versions'."
        )]
            string uri,
        CancellationToken cancellationToken = default
    )
    {
        // Extract package ID from URI (format: nuget://packages/{packageId}/versions)
        var parts = uri.Replace("nuget://packages/", "", StringComparison.OrdinalIgnoreCase)
            .Split('/');
        var packageId = parts.Length > 0 ? parts[0] : "";

        if (string.IsNullOrWhiteSpace(packageId))
        {
            return new
            {
                uri,
                found = false,
                mimeType = "application/json",
                message = "Invalid URI format. Expected: nuget://packages/{packageId}/versions",
            };
        }

        var versions = await nugetService.GetPackageVersionsAsync(packageId, cancellationToken);

        return new
        {
            uri,
            mimeType = "application/json",
            found = true,
            packageId,
            versions = versions.Select(v => new
            {
                version = v.Version,
                downloadCount = v.DownloadCount,
                isPrerelease = v.IsPrerelease,
                isDeprecated = v.IsDeprecated,
            }),
            totalCount = versions.Count,
        };
    }

    /// <summary>
    /// Get the README content for a NuGet package as a resource
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="uri">The resource URI in format: nuget://packages/PackageId/readme or nuget://packages/PackageId/readme?version=1.0.0</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Package README content</returns>
    [McpServerResource]
    [Description(
        "Access README documentation for a NuGet package. README files typically contain installation instructions, quick start guides, API overview, usage examples, and links to detailed documentation. Use this to understand how to use a package, see code examples, learn about package features, and check requirements before loading."
    )]
    public static async Task<object> GetPackageReadme(
        NuGetService nugetService,
        [Description(
            "Resource URI in the format 'nuget://packages/PackageId/readme' or 'nuget://packages/PackageId/readme?version=1.0.0'. Examples: 'nuget://packages/Newtonsoft.Json/readme', 'nuget://packages/Flurl.Http/readme?version=4.0.0'."
        )]
            string uri,
        CancellationToken cancellationToken = default
    )
    {
        // Extract package ID and version from URI
        var uriObj = new Uri(uri.StartsWith("nuget://", StringComparison.OrdinalIgnoreCase)
            ? uri
            : $"nuget://{uri}");

        var path = uriObj.AbsolutePath.Replace("/packages/", "", StringComparison.OrdinalIgnoreCase);
        var parts = path.Split('/');
        var packageId = parts.Length > 0 ? parts[0] : "";

        if (string.IsNullOrWhiteSpace(packageId))
        {
            return new
            {
                uri,
                found = false,
                mimeType = "text/markdown",
                message = "Invalid URI format. Expected: nuget://packages/{packageId}/readme",
            };
        }

        var query = ParseQueryString(uriObj.Query);
        var version = query.TryGetValue("version", out var v) ? v : null;

        var readme = await nugetService.GetPackageReadmeAsync(
            packageId,
            version,
            cancellationToken
        );

        return new
        {
            uri,
            mimeType = "text/markdown",
            found = readme != null,
            packageId,
            version = version ?? "latest",
            content = readme ?? "README not found",
        };
    }

    /// <summary>
    /// Parse query string into dictionary using URL decoding
    /// </summary>
    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return result;

        query = query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }
        return result;
    }
}
