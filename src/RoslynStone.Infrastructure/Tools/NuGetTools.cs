using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP tools for NuGet package operations
/// </summary>
[McpServerToolType]
public class NuGetTools
{
    /// <summary>
    /// Load a NuGet package into the REPL environment
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="packageName">The package name to load</param>
    /// <param name="version">Optional specific version (uses latest stable if not specified)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing the package name, version, and whether it was successfully loaded</returns>
    [McpServerTool]
    [Description(
        "Load a NuGet package and all its dependencies into the REPL environment, making types and methods available for use in subsequent code executions. This downloads the package (if needed), resolves dependencies, and adds assemblies to the REPL. After loading, use 'using' directives to access the package's namespaces. Use this to: extend the REPL with external libraries, add functionality for JSON/HTTP/CSV/etc., leverage popular packages, and build complex solutions. Package remains loaded until ResetRepl is called."
    )]
    public static async Task<object> LoadNuGetPackage(
        RoslynScriptingService scriptingService,
        NuGetService nugetService,
        [Description(
            "The exact package name to load. Examples: 'Newtonsoft.Json', 'Flurl.Http', 'CsvHelper'. Must match the package ID from search results. Case-insensitive."
        )]
            string packageName,
        [Description(
            "Optional specific version to load (e.g., '13.0.3', '2.1.0'). If not specified, loads the latest stable (non-prerelease) version. Use GetNuGetPackageVersions to see available versions. Prefer stable versions for production use."
        )]
            string? version = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var assemblyPaths = await nugetService.DownloadPackageAsync(
                packageName,
                version,
                cancellationToken
            );

            await scriptingService.AddPackageReferenceAsync(packageName, version, assemblyPaths);

            return new
            {
                packageName,
                version = version ?? "latest",
                isLoaded = true,
                message = $"Package '{packageName}' version '{version ?? "latest"}' loaded successfully",
            };
        }
        catch (InvalidOperationException ex)
        {
            return new
            {
                packageName,
                version = version ?? "unspecified",
                isLoaded = false,
                message = $"Package not found: {ex.Message}",
            };
        }
        catch (IOException ex)
        {
            return new
            {
                packageName,
                version = version ?? "unspecified",
                isLoaded = false,
                message = $"File access error: {ex.Message}",
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new
            {
                packageName,
                version = version ?? "unspecified",
                isLoaded = false,
                message = $"Failed to load package: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Search for NuGet packages
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="query">Search query (keywords, package names, tags, or descriptions)</param>
    /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
    /// <param name="take">Number of results to return (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Search results with package metadata</returns>
    [McpServerTool]
    [Description(
        "Search the NuGet package repository catalog to find libraries and tools. Returns matching packages with metadata including name, description, authors, latest version, download count, and URLs. Use this to discover packages for specific functionality, find popular libraries, and check package information before loading. Search by keywords, package names, tags, or descriptions."
    )]
    public static async Task<object> SearchNuGetPackages(
        NuGetService nugetService,
        [Description(
            "Search query. Examples: 'json', 'http client', 'Newtonsoft.Json', 'csv parser', 'logging'"
        )]
            string query,
        [Description("Number of results to skip for pagination. Default: 0")] int skip = 0,
        [Description("Number of results to return. Default: 20, max: 100")] int take = 20,
        CancellationToken cancellationToken = default
    )
    {
        // Clamp take to valid range
        take = Math.Min(Math.Max(take, 1), 100);

        var result = await nugetService.SearchPackagesAsync(query, skip, take, cancellationToken);

        return new
        {
            packages = result
                .Packages.Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    description = p.Description,
                    authors = p.Authors,
                    latestVersion = p.LatestVersion,
                    downloadCount = p.DownloadCount ?? 0,
                    iconUrl = p.IconUrl,
                    projectUrl = p.ProjectUrl,
                    tags = p.Tags,
                })
                .ToList(),
            totalCount = result.TotalCount,
            query,
            skip,
            take,
        };
    }

    /// <summary>
    /// Get available versions for a NuGet package
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="packageId">The package ID to get versions for</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>List of package versions with metadata</returns>
    [McpServerTool]
    [Description(
        "Access all published versions of a specific NuGet package, sorted from newest to oldest. Returns version numbers, download counts, and flags indicating prerelease or deprecated status. Use this to choose a specific version to load, check version history, avoid deprecated versions, and identify stable releases."
    )]
    public static async Task<object> GetNuGetPackageVersions(
        NuGetService nugetService,
        [Description(
            "Package ID to get versions for. Examples: 'Newtonsoft.Json', 'Microsoft.Extensions.Logging'"
        )]
            string packageId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var versions = await nugetService.GetPackageVersionsAsync(packageId, cancellationToken);

            return new
            {
                found = true,
                packageId,
                versions = versions
                    .Select(v => new
                    {
                        version = v.Version,
                        downloadCount = v.DownloadCount ?? 0,
                        isPrerelease = v.IsPrerelease,
                        isDeprecated = v.IsDeprecated,
                    })
                    .ToList(),
                totalCount = versions.Count,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new
            {
                found = false,
                packageId,
                message = $"Failed to retrieve versions for package '{packageId}': {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Get the README content for a NuGet package
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="packageId">The package ID to get README for</param>
    /// <param name="version">Optional specific version (uses latest if not specified)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Package README content</returns>
    [McpServerTool]
    [Description(
        "Access README documentation for a NuGet package. README files typically contain installation instructions, quick start guides, API overview, usage examples, and links to detailed documentation. Use this to understand how to use a package, see code examples, learn about package features, and check requirements before loading."
    )]
    public static async Task<object> GetNuGetPackageReadme(
        NuGetService nugetService,
        [Description(
            "Package ID to get README for. Examples: 'Newtonsoft.Json', 'Flurl.Http', 'CsvHelper'"
        )]
            string packageId,
        [Description("Optional specific version. If not specified, uses latest version.")]
            string? version = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var readme = await nugetService.GetPackageReadmeAsync(
                packageId,
                version,
                cancellationToken
            );

            return new
            {
                found = readme != null,
                packageId,
                version = version ?? "latest",
                content = readme ?? "README not found for this package",
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new
            {
                found = false,
                packageId,
                version = version ?? "latest",
                message = $"Error retrieving README: {ex.Message}",
            };
        }
    }
}
