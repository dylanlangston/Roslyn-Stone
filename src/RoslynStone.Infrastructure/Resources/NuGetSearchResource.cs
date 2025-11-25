using System.ComponentModel;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Protocol;
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
    /// <param name="requestContext">The request context containing URI and cancellation token</param>
    /// <param name="query">Search query. Use this to search by package name, keyword, or description.</param>
    /// <param name="skip">Optional number of records to skip (default: 0)</param>
    /// <param name="take">Optional number of records to return (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Search results with package metadata</returns>
    [McpServerResource(UriTemplate = "nuget://search?q={query}&skip={skip}&take={take}", Name = "NuGet Package Search", MimeType = "application/json")]
    [Description(
        "Access the NuGet package repository catalog to find libraries and tools. Returns matching packages with metadata including name, description, authors, latest version, download count, and URLs. Use this to discover packages for specific functionality, find popular libraries, and check package information before loading. Search by keywords, package names, tags, or descriptions. URI format: nuget://search?q={query}&skip={skip}&take={take}"
    )]
    public static async Task<ResourceContents> SearchPackages(
        NuGetService nugetService,
        RequestContext<ReadResourceRequestParams> requestContext,
        [Description("Search query; examples: 'json', 'http client', 'Newtonsoft.Json', 'csv parser', 'logging'.")]
            string query,
        [Description("Optional number of records to skip; default: 0")]
            int? skip = null,
        [Description("Optional number of records to take; default: 20, max: 100")]
            int? take = null,
        CancellationToken cancellationToken = default
    )
    {
        var uri = requestContext.Params?.Uri ?? "nuget://search";

        // Finalize defaults if still null
        var finalQuery = query ?? string.Empty;
        var finalSkip = skip ?? 0;
        var finalTake = take.HasValue ? Math.Min(take.Value, 100) : 20;

        var result = await nugetService.SearchPackagesAsync(
            finalQuery,
            finalSkip,
            finalTake,
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
            Skip = finalSkip,
            Take = finalTake,
        };
    }
}
