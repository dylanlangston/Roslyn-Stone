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
}
