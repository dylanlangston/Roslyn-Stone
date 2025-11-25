using System.ComponentModel;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Protocol;
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
    /// <param name="requestContext">The request context containing URI and cancellation token</param>
    /// <param name="id">The package ID extracted from the URI</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>List of package versions with metadata</returns>
    [McpServerResource(UriTemplate = "nuget://packages/{id}/versions", Name = "NuGet Package Versions", MimeType = "application/json")]
    [Description(
        "Access all published versions of a specific NuGet package, sorted from newest to oldest. Returns version numbers, download counts, and flags indicating prerelease or deprecated status. Use this to choose a specific version to load, check version history, avoid deprecated versions, and identify stable releases. URI format: nuget://packages/{PackageId}/versions"
    )]
    public static async Task<ResourceContents> GetPackageVersions(
        NuGetService nugetService,
        RequestContext<ReadResourceRequestParams> requestContext,
        [Description(
            "The package ID extracted from the URI. Example: 'Newtonsoft.Json', 'Serilog', 'Dapper'."
        )]
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var uri = requestContext.Params?.Uri ?? $"nuget://packages/{id}/readme";

        var versions = await nugetService.GetPackageVersionsAsync(id, cancellationToken);

        return new PackageVersionsResponse
        {
            Uri = uri,
            MimeType = "application/json",
            Found = true,
            PackageId = id,
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
    /// <param name="requestContext">The request context containing URI and cancellation token</param>
    /// <param name="id">The package ID extracted from the URI</param>
    /// <param name="version">Optional package version to load</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Package README content</returns>
    [McpServerResource(UriTemplate = "nuget://packages/{id}/readme?version={version}", Name = "NuGet Package README", MimeType = "text/markdown")]
    [Description(
        "Access README documentation for a NuGet package. README files typically contain installation instructions, quick start guides, API overview, usage examples, and links to detailed documentation. Use this to understand how to use a package, see code examples, learn about package features, and check requirements before loading. URI format: nuget://packages/{PackageId}/readme?version={version}"
    )]
    public static async Task<ResourceContents> GetPackageReadme(
        NuGetService nugetService,
        RequestContext<ReadResourceRequestParams> requestContext,
        [Description(
            "The package ID extracted from the URI. Example: 'Newtonsoft.Json', 'Serilog', 'Dapper'."
        )]
        string id,
        [Description(
            "Optional package version to load. Example: '13.0.1'"
        )]
        string? version = null,
        CancellationToken cancellationToken = default
    )
    {
        var uri = requestContext.Params?.Uri ?? $"nuget://packages/{id}/readme";

        var readme = await nugetService.GetPackageReadmeAsync(id, version, cancellationToken);

        return new PackageReadmeResponse
        {
            Uri = uri,
            MimeType = "text/markdown",
            Found = readme != null,
            PackageId = id,
            Version = version ?? "latest",
            Content = readme ?? "README not found for this package",
        };
    }
}
