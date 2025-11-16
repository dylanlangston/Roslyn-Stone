namespace RoslynStone.Core.Models;

/// <summary>
/// Represents metadata for a NuGet package
/// </summary>
public class PackageMetadata
{
    /// <summary>
    /// Gets or sets the package ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the package description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the package authors
    /// </summary>
    public string? Authors { get; set; }

    /// <summary>
    /// Gets or sets the latest version
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Gets or sets the total download count
    /// </summary>
    public long? DownloadCount { get; set; }

    /// <summary>
    /// Gets or sets the package icon URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the package project URL
    /// </summary>
    public string? ProjectUrl { get; set; }

    /// <summary>
    /// Gets or sets the package tags
    /// </summary>
    public string? Tags { get; set; }
}
