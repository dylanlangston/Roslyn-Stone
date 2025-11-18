using System.ComponentModel;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Resources;

/// <summary>
/// MCP resource for NuGet package search
/// </summary>
[McpServerResourceType]
public class NuGetSearchResource
{
    /// <summary>
    /// Search for NuGet packages as a resource
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="uri">The resource URI in format: nuget://search?q=query&amp;skip=0&amp;take=20</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Search results with package metadata</returns>
    [McpServerResource]
    [Description(
        "Access the NuGet package repository catalog to find libraries and tools. Returns matching packages with metadata including name, description, authors, latest version, download count, and URLs. Use this to discover packages for specific functionality, find popular libraries, and check package information before loading. Search by keywords, package names, tags, or descriptions. URI format: nuget://search?q={query}&skip={skip}&take={take}"
    )]
    public static async Task<PackageSearchResponse> SearchPackages(
        NuGetService nugetService,
        [Description(
            "Resource URI in the format 'nuget://search?q=query&skip=0&take=20'. Query parameter examples: 'json', 'http client', 'Newtonsoft.Json', 'csv parser', 'logging'."
        )]
            string uri,
        CancellationToken cancellationToken = default
    )
    {
        // Parse URI and extract query parameters
        var uriObj = new Uri(
            uri.StartsWith("nuget://", StringComparison.OrdinalIgnoreCase) ? uri : $"nuget://{uri}"
        );

        var query = QueryHelpers.ParseQuery(uriObj.Query);
        var searchQuery = query.TryGetValue("q", out var q) ? q.ToString() : "";
        var skip =
            query.TryGetValue("skip", out var skipStr) && int.TryParse(skipStr, out var s) ? s : 0;
        var take =
            query.TryGetValue("take", out var takeStr) && int.TryParse(takeStr, out var t)
                ? Math.Min(t, 100)
                : 20;

        var result = await nugetService.SearchPackagesAsync(
            searchQuery,
            skip,
            take,
            cancellationToken
        );

        return new PackageSearchResponse
        {
            Uri = uri,
            MimeType = "application/json",
            Packages = result
                .Packages.Select(p => new PackageInfo
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Authors = p.Authors,
                    LatestVersion = p.LatestVersion,
                    // Defensive: DownloadCount may be null for newly published packages or due to data sync issues with NuGet API
                    DownloadCount = p.DownloadCount ?? 0,
                    IconUrl = p.IconUrl,
                    ProjectUrl = p.ProjectUrl,
                    Tags = p.Tags,
                })
                .ToList(),
            TotalCount = result.TotalCount,
            Query = result.Query,
            Skip = skip,
            Take = take,
        };
    }
}
