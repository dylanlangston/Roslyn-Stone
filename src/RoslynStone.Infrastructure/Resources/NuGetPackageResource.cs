using System.ComponentModel;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Models;
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
        "Access all published versions of a specific NuGet package, sorted from newest to oldest. Returns version numbers, download counts, and flags indicating prerelease or deprecated status. Use this to choose a specific version to load, check version history, avoid deprecated versions, and identify stable releases. URI format: nuget://packages/{PackageId}/versions"
    )]
    public static async Task<PackageVersionsResponse> GetPackageVersions(
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
            return new PackageVersionsResponse
            {
                Uri = uri,
                Found = false,
                MimeType = "application/json",
                Message =
                    "Invalid URI format. Expected: nuget://packages/{packageId}/versions. Example: nuget://packages/Newtonsoft.Json/versions",
            };
        }

        var versions = await nugetService.GetPackageVersionsAsync(packageId, cancellationToken);

        return new PackageVersionsResponse
        {
            Uri = uri,
            MimeType = "application/json",
            Found = true,
            PackageId = packageId,
            Versions = versions
                .Select(v => new PackageVersionInfo
                {
                    Version = v.Version,
                    DownloadCount = v.DownloadCount ?? 0,
                    IsPrerelease = v.IsPrerelease,
                    IsDeprecated = v.IsDeprecated,
                })
                .ToList(),
            TotalCount = versions.Count,
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
        "Access README documentation for a NuGet package. README files typically contain installation instructions, quick start guides, API overview, usage examples, and links to detailed documentation. Use this to understand how to use a package, see code examples, learn about package features, and check requirements before loading. URI format: nuget://packages/{PackageId}/readme?version={version}"
    )]
    public static async Task<PackageReadmeResponse> GetPackageReadme(
        NuGetService nugetService,
        [Description(
            "Resource URI in the format 'nuget://packages/PackageId/readme' or 'nuget://packages/PackageId/readme?version=1.0.0'. Examples: 'nuget://packages/Newtonsoft.Json/readme', 'nuget://packages/Flurl.Http/readme?version=4.0.0'."
        )]
            string uri,
        CancellationToken cancellationToken = default
    )
    {
        // Extract package ID and version from URI
        var uriObj = new Uri(
            uri.StartsWith("nuget://", StringComparison.OrdinalIgnoreCase) ? uri : $"nuget://{uri}"
        );

        var path = uriObj.AbsolutePath.Replace(
            "/packages/",
            "",
            StringComparison.OrdinalIgnoreCase
        );
        var parts = path.Split('/');
        var packageId = parts.Length > 0 ? parts[0] : "";

        if (string.IsNullOrWhiteSpace(packageId))
        {
            return new PackageReadmeResponse
            {
                Uri = uri,
                Found = false,
                MimeType = "text/markdown",
                Message =
                    "Invalid URI format. Expected: nuget://packages/{packageId}/readme. Example: nuget://packages/Newtonsoft.Json/readme",
            };
        }

        var query = QueryHelpers.ParseQuery(uriObj.Query);
        var version = query.TryGetValue("version", out var v) ? v.ToString() : null;

        var readme = await nugetService.GetPackageReadmeAsync(
            packageId,
            version,
            cancellationToken
        );

        return new PackageReadmeResponse
        {
            Uri = uri,
            MimeType = "text/markdown",
            Found = readme != null,
            PackageId = packageId,
            Version = version ?? "latest",
            Content = readme ?? "README not found for this package",
        };
    }
}
