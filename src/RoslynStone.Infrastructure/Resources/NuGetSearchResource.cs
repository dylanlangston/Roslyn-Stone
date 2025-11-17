using System.ComponentModel;
using System.Text.Encodings.Web;
using ModelContextProtocol.Server;
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
        "Access the NuGet package repository catalog to find libraries and tools. Returns matching packages with metadata including name, description, authors, latest version, download count, and URLs. Use this to discover packages for specific functionality, find popular libraries, and check package information before loading. Search by keywords, package names, tags, or descriptions."
    )]
    public static async Task<object> SearchPackages(
        NuGetService nugetService,
        [Description(
            "Resource URI in the format 'nuget://search?q=query&amp;skip=0&amp;take=20'. Query parameter examples: 'json', 'http client', 'Newtonsoft.Json', 'csv parser', 'logging'."
        )]
            string uri,
        CancellationToken cancellationToken = default
    )
    {
        // Parse URI and extract query parameters
        var uriObj = new Uri(uri.StartsWith("nuget://", StringComparison.OrdinalIgnoreCase)
            ? uri
            : $"nuget://{uri}");

        var query = ParseQueryString(uriObj.Query);
        var searchQuery = query.TryGetValue("q", out var q) ? q : "";
        var skip = query.TryGetValue("skip", out var skipStr) && int.TryParse(skipStr, out var s) ? s : 0;
        var take = query.TryGetValue("take", out var takeStr) && int.TryParse(takeStr, out var t) ? Math.Min(t, 100) : 20;

        var result = await nugetService.SearchPackagesAsync(
            searchQuery,
            skip,
            take,
            cancellationToken
        );

        return new
        {
            uri,
            mimeType = "application/json",
            packages = result.Packages.Select(p => new
            {
                id = p.Id,
                title = p.Title,
                description = p.Description,
                authors = p.Authors,
                latestVersion = p.LatestVersion,
                downloadCount = p.DownloadCount,
                iconUrl = p.IconUrl,
                projectUrl = p.ProjectUrl,
                tags = p.Tags,
            }),
            totalCount = result.TotalCount,
            query = result.Query,
            skip,
            take,
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
