using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using RoslynStone.Core.Models;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for NuGet package operations including search, version lookup, and package loading
/// </summary>
public class NuGetService : IDisposable
{
    private readonly SourceRepository _repository;
    private readonly SourceCacheContext _cache;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NuGetService"/> class
    /// </summary>
    public NuGetService()
    {
        _logger = NullLogger.Instance;
        _repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        _cache = new SourceCacheContext();
    }

    /// <summary>
    /// Search for NuGet packages
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="skip">Number of results to skip</param>
    /// <param name="take">Number of results to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Package search results</returns>
    public async Task<PackageSearchResult> SearchPackagesAsync(
        string query,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default
    )
    {
        var searchResource = await _repository.GetResourceAsync<PackageSearchResource>(
            cancellationToken
        );

        var searchFilter = new SearchFilter(includePrerelease: true);

        var results = await searchResource.SearchAsync(
            query,
            searchFilter,
            skip,
            take,
            _logger,
            cancellationToken
        );

        var packages = results
            .Select(result => new PackageMetadata
            {
                Id = result.Identity.Id,
                Title = result.Title,
                Description = result.Description,
                Authors = result.Authors,
                LatestVersion = result.Identity.Version.ToString(),
                DownloadCount = result.DownloadCount,
                IconUrl = result.IconUrl?.ToString(),
                ProjectUrl = result.ProjectUrl?.ToString(),
                Tags = result.Tags,
            })
            .ToList();

        return new PackageSearchResult
        {
            Packages = packages,
            TotalCount = packages.Count,
            Query = query,
        };
    }

    /// <summary>
    /// Get all versions of a package
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of package versions</returns>
    public async Task<List<PackageVersion>> GetPackageVersionsAsync(
        string packageId,
        CancellationToken cancellationToken = default
    )
    {
        var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>(
            cancellationToken
        );

        var metadata = await metadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            _cache,
            _logger,
            cancellationToken
        );

        return metadata
            .Select(item => new PackageVersion
            {
                Version = item.Identity.Version.ToString(),
                DownloadCount = item.DownloadCount,
                IsPrerelease = item.Identity.Version.IsPrerelease,
                IsDeprecated = !item.IsListed,
            })
            .OrderByDescending(v => NuGetVersion.Parse(v.Version))
            .ToList();
    }

    /// <summary>
    /// Get the README content for a package
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="version">Package version (optional, uses latest if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>README content as string, or null if not found</returns>
    public async Task<string?> GetPackageReadmeAsync(
        string packageId,
        string? version = null,
        CancellationToken cancellationToken = default
    )
    {
        // Get package metadata to find the version
        var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>(
            cancellationToken
        );

        var metadata = await metadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            _cache,
            _logger,
            cancellationToken
        );

        var metadataList = metadata.ToList();
        IPackageSearchMetadata? packageMetadata;
        if (string.IsNullOrEmpty(version))
        {
            // Get the latest version
            packageMetadata = metadataList
                .OrderByDescending(m => m.Identity.Version)
                .FirstOrDefault();
        }
        else
        {
            // Find specific version
            var targetVersion = NuGetVersion.Parse(version);
            packageMetadata = metadataList.FirstOrDefault(m => m.Identity.Version == targetVersion);
        }

        if (packageMetadata == null)
        {
            return null;
        }

        // Try to download the package and extract README
        var downloadResource = await _repository.GetResourceAsync<DownloadResource>(
            cancellationToken
        );

        using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
            packageMetadata.Identity,
            new PackageDownloadContext(_cache),
            SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null)),
            _logger,
            cancellationToken
        );

        if (downloadResult.Status != DownloadResourceResultStatus.Available)
        {
            return null;
        }

        // Extract and read README from the package
        using var packageStream = downloadResult.PackageStream;
        using var packageReader = new PackageArchiveReader(packageStream);

        // Try to find README file
        var readmeFiles = packageReader
            .GetFiles()
            .Where(f =>
                f.EndsWith("README.md", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith("README.txt", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith("README", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        if (readmeFiles.Count == 0)
        {
            return null;
        }

        // Prefer README.md, then README.txt, then just README
        var readmeFile =
            readmeFiles.FirstOrDefault(f =>
                f.EndsWith("README.md", StringComparison.OrdinalIgnoreCase)
            )
            ?? readmeFiles.FirstOrDefault(f =>
                f.EndsWith("README.txt", StringComparison.OrdinalIgnoreCase)
            )
            ?? readmeFiles.First();

        using var readmeStream = packageReader.GetStream(readmeFile);
        using var reader = new StreamReader(readmeStream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Download and get assembly paths for a package and its dependencies
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="version">Package version (optional, uses latest if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of assembly file paths</returns>
    public async Task<List<string>> DownloadPackageAsync(
        string packageId,
        string? version = null,
        CancellationToken cancellationToken = default
    )
    {
        // Get package metadata
        var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>(
            cancellationToken
        );

        var metadata = await metadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            _cache,
            _logger,
            cancellationToken
        );

        var metadataList = metadata.ToList();

        var packageMetadata = string.IsNullOrEmpty(version)
            ? metadataList
                .Where(m => !m.Identity.Version.IsPrerelease)
                .MaxBy(m => m.Identity.Version)
            ?? metadataList.MaxBy(m => m.Identity.Version)
            : metadataList.FirstOrDefault(m => m.Identity.Version == NuGetVersion.Parse(version));

        if (packageMetadata == null)
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' version '{version ?? "latest"}' not found"
            );
        }

        // Download package
        var downloadResource = await _repository.GetResourceAsync<DownloadResource>(
            cancellationToken
        );

        var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(
            Settings.LoadDefaultSettings(null)
        );

        using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
            packageMetadata.Identity,
            new PackageDownloadContext(_cache),
            globalPackagesFolder,
            _logger,
            cancellationToken
        );

        if (downloadResult.Status != DownloadResourceResultStatus.Available)
        {
            throw new InvalidOperationException(
                $"Failed to download package '{packageId}' version '{packageMetadata.Identity.Version}'"
            );
        }

        // Extract assemblies from the package
        var assemblies = new List<string>();
        using var packageStream = downloadResult.PackageStream;
        using var packageReader = new PackageArchiveReader(packageStream);

        // Get lib files that match the current framework
        var libItems = packageReader.GetLibItems().ToList();

        // Use NuGet framework compatibility logic to select the best framework
        var currentFramework = NuGetFramework.Parse($"net{Environment.Version.Major}.0");
        var reducer = new FrameworkReducer();
        var nearestFramework = reducer.GetNearest(
            currentFramework,
            libItems.Select(l => l.TargetFramework)
        );

        var targetFramework =
            nearestFramework != null
                ? libItems.FirstOrDefault(l => l.TargetFramework.Equals(nearestFramework))
                : libItems.OrderByDescending(g => g.TargetFramework.Version).FirstOrDefault();

        if (targetFramework != null)
        {
            var packagePath = Path.Combine(
                globalPackagesFolder,
                packageId.ToLowerInvariant(),
                packageMetadata.Identity.Version.ToNormalizedString()
            );

            assemblies.AddRange(
                targetFramework
                    .Items.Where(f =>
                        f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        && !f.Replace('\\', '/').Contains("/ref/")
                    )
                    .Select(file => Path.Combine(packagePath, file))
                    .Where(File.Exists)
            );
        }

        return assemblies;
    }

    /// <summary>
    /// Disposes the NuGet service and releases resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.Dispose();
            _disposed = true;
        }
    }
}
