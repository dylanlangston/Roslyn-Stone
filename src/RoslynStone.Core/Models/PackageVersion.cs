namespace RoslynStone.Core.Models;

/// <summary>
/// Represents a specific version of a NuGet package
/// </summary>
public class PackageVersion
{
    /// <summary>
    /// Gets or sets the version string
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the download count for this version
    /// </summary>
    public long? DownloadCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a prerelease version
    /// </summary>
    public bool IsPrerelease { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this version is deprecated
    /// </summary>
    public bool IsDeprecated { get; set; }
}
