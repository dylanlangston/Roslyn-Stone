using System.ComponentModel;
using Microsoft.Extensions.Logging;
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
    /// Search for NuGet packages
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="query">Search query string</param>
    /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
    /// <param name="take">Number of results to return (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing search results with package metadata, total count, and query</returns>
    [McpServerTool]
    [Description(
        "Search the NuGet package repository to find libraries and tools. Returns matching packages with metadata including name, description, authors, latest version, download count, and URLs. Use this to: discover packages for specific functionality (JSON, HTTP, CSV, etc.), find popular libraries, explore package ecosystems, and check package information before loading. Search by keywords, package names, tags, or descriptions. Results are ranked by relevance and popularity."
    )]
    public static async Task<object> SearchNuGetPackages(
        NuGetService nugetService,
        [Description(
            "Search keywords or package name. Examples: 'json' (finds JSON libraries), 'http client' (finds HTTP libraries), 'Newtonsoft.Json' (exact package), 'csv parser', 'logging'. Searches across package names, descriptions, and tags."
        )]
            string query,
        [Description(
            "Number of results to skip for pagination. Use 0 for first page, 20 for second page, etc. Default: 0"
        )]
            int skip = 0,
        [Description(
            "Number of results to return per page. Range: 1-100. Default: 20. Use smaller values for quick searches, larger values for comprehensive results."
        )]
            int take = 20,
        CancellationToken cancellationToken = default
    )
    {
        var result = await nugetService.SearchPackagesAsync(
            query,
            skip,
            Math.Min(take, 100),
            cancellationToken
        );

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
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="packageId">The package ID to get versions for</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing a list of versions with metadata including version string, download count, and prerelease/deprecated flags</returns>
    [McpServerTool]
    [Description(
        "Retrieve all published versions of a specific NuGet package, sorted from newest to oldest. Returns version numbers, download counts, and flags indicating prerelease or deprecated status. Use this to: choose a specific version to load, check version history, avoid deprecated versions, identify stable releases, and understand package evolution. Essential before calling LoadNuGetPackage with a specific version."
    )]
    public static async Task<object> GetNuGetPackageVersions(
        NuGetService nugetService,
        [Description(
            "The exact package ID (case-insensitive). Examples: 'Newtonsoft.Json', 'Microsoft.Extensions.Logging', 'AutoMapper'. Must match the package ID exactly as it appears in search results."
        )]
            string packageId,
        CancellationToken cancellationToken = default
    )
    {
        var versions = await nugetService.GetPackageVersionsAsync(packageId, cancellationToken);

        return new
        {
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
    /// Get the README content for a NuGet package
    /// </summary>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="packageId">The package ID to get README for</param>
    /// <param name="version">Optional specific version (uses latest if not specified)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing the package ID, version, and README content</returns>
    [McpServerTool]
    [Description(
        "Retrieve the README documentation for a NuGet package. README files typically contain installation instructions, quick start guides, API overview, usage examples, and links to detailed documentation. Use this to: understand how to use a package, see code examples, learn about package features, check requirements and compatibility, and get started quickly. Read this BEFORE loading a package to ensure it meets your needs."
    )]
    public static async Task<object> GetNuGetPackageReadme(
        NuGetService nugetService,
        [Description(
            "The exact package ID to get README for. Examples: 'Newtonsoft.Json', 'Flurl.Http', 'CsvHelper'."
        )]
            string packageId,
        [Description(
            "Optional specific version (e.g., '13.0.3', '2.1.0'). If not specified, retrieves README from the latest stable version. Use GetNuGetPackageVersions to see available versions."
        )]
            string? version = null,
        CancellationToken cancellationToken = default
    )
    {
        var readme = await nugetService.GetPackageReadmeAsync(
            packageId,
            version,
            cancellationToken
        );

        return new
        {
            packageId,
            version = version ?? "latest",
            readme = readme ?? "README not found",
            found = readme != null,
        };
    }

    /// <summary>
    /// Load a NuGet package into the REPL environment
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="logger">Logger for diagnostics</param>
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
        Microsoft.Extensions.Logging.ILogger logger,
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
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to load package '{PackageName}' version '{Version}': {ErrorMessage}",
                packageName,
                version ?? "latest",
                ex.Message
            );

            return new
            {
                packageName,
                version = version ?? "unspecified",
                isLoaded = false,
                message = $"Failed to load package '{packageName}': {ex.Message}",
            };
        }
    }
}
