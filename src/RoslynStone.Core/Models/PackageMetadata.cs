namespace RoslynStone.Core.Models;

/// <summary>
/// Represents metadata for a NuGet package
/// </summary>
public record PackageMetadata
{
    /// <summary>
    /// Gets the package ID
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the package title
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the package description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the package authors
    /// </summary>
    public string? Authors { get; init; }

    /// <summary>
    /// Gets the latest version
    /// </summary>
    public string? LatestVersion { get; init; }

    /// <summary>
    /// Gets the total download count
    /// </summary>
    public long? DownloadCount { get; init; }

    /// <summary>
    /// Gets the package icon URL
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// Gets the package project URL
    /// </summary>
    public string? ProjectUrl { get; init; }

    /// <summary>
    /// Gets the package tags
    /// </summary>
    public string? Tags { get; init; }
}
