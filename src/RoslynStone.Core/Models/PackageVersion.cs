namespace RoslynStone.Core.Models;

/// <summary>
/// Represents a specific version of a NuGet package
/// </summary>
public record PackageVersion
{
    /// <summary>
    /// Gets the version string
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the download count for this version
    /// </summary>
    public long? DownloadCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a prerelease version
    /// </summary>
    public required bool IsPrerelease { get; init; }

    /// <summary>
    /// Gets a value indicating whether this version is deprecated
    /// </summary>
    public required bool IsDeprecated { get; init; }
}
