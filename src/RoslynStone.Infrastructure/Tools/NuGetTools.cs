using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Queries;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP tools for NuGet package operations
/// </summary>
[McpServerToolType]
public class NuGetTools
{
    /// <summary>
    /// Search for NuGet packages
    /// </summary>
    /// <param name="queryHandler">The query handler for searching packages</param>
    /// <param name="query">Search query string</param>
    /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
    /// <param name="take">Number of results to return (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing search results with package metadata, total count, and query</returns>
    [McpServerTool]
    [Description("Search for NuGet packages by name, description, or tags")]
    public static async Task<object> SearchNuGetPackages(
        IQueryHandler<SearchPackagesQuery, Core.Models.PackageSearchResult> queryHandler,
        [Description("Search query string")] string query,
        [Description("Number of results to skip (default: 0)")] int skip = 0,
        [Description("Number of results to return (default: 20)")] int take = 20,
        CancellationToken cancellationToken = default
    )
    {
        var searchQuery = new SearchPackagesQuery(query, skip, Math.Min(take, 100));
        var result = await queryHandler.HandleAsync(searchQuery, cancellationToken);

        return new
        {
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
        };
    }

    /// <summary>
    /// Get all available versions of a NuGet package
    /// </summary>
    /// <param name="queryHandler">The query handler for getting package versions</param>
    /// <param name="packageId">The package ID to get versions for</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing a list of versions with metadata including version string, download count, and prerelease/deprecated flags</returns>
    [McpServerTool]
    [Description("Get all available versions of a NuGet package")]
    public static async Task<object> GetNuGetPackageVersions(
        IQueryHandler<GetPackageVersionsQuery, List<Core.Models.PackageVersion>> queryHandler,
        [Description("Package ID to get versions for")] string packageId,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetPackageVersionsQuery(packageId);
        var versions = await queryHandler.HandleAsync(query, cancellationToken);

        return new
        {
            packageId = packageId,
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
    /// Get the README content for a NuGet package
    /// </summary>
    /// <param name="queryHandler">The query handler for getting package README</param>
    /// <param name="packageId">The package ID to get README for</param>
    /// <param name="version">Optional specific version (uses latest if not specified)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing the package ID, version, and README content</returns>
    [McpServerTool]
    [Description("Get the README content for a NuGet package")]
    public static async Task<object> GetNuGetPackageReadme(
        IQueryHandler<GetPackageReadmeQuery, string?> queryHandler,
        [Description("Package ID to get README for")] string packageId,
        [Description("Optional version (uses latest if not specified)")] string? version = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetPackageReadmeQuery(packageId, version);
        var readme = await queryHandler.HandleAsync(query, cancellationToken);

        return new
        {
            packageId = packageId,
            version = version ?? "latest",
            readme = readme ?? "README not found",
            found = readme != null,
        };
    }

    /// <summary>
    /// Load a NuGet package into the REPL environment
    /// </summary>
    /// <param name="commandHandler">The command handler for loading packages</param>
    /// <param name="packageName">The package name to load</param>
    /// <param name="version">Optional specific version (uses latest stable if not specified)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing the package name, version, and whether it was successfully loaded</returns>
    [McpServerTool]
    [Description(
        "Load a NuGet package into the REPL environment, making its types and methods available for use"
    )]
    public static async Task<object> LoadNuGetPackage(
        ICommandHandler<LoadPackageCommand, Core.Models.PackageReference> commandHandler,
        [Description("Package name to load")] string packageName,
        [Description("Optional version (uses latest stable if not specified)")] string? version =
            null,
        CancellationToken cancellationToken = default
    )
    {
        var command = new LoadPackageCommand(packageName, version);
        var result = await commandHandler.HandleAsync(command, cancellationToken);

        return new
        {
            packageName = result.Name,
            version = result.Version,
            isLoaded = result.IsLoaded,
            message = result.IsLoaded
                ? $"Package '{result.Name}' version '{result.Version ?? "latest"}' loaded successfully"
                : $"Failed to load package '{result.Name}' version '{result.Version ?? "latest"}'",
        };
    }
}
