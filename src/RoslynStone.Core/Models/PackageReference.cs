namespace RoslynStone.Core.Models;

/// <summary>
/// Represents a NuGet package reference
/// </summary>
public class PackageReference
{
    /// <summary>
    /// Gets or sets the package name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package is loaded
    /// </summary>
    public bool IsLoaded { get; set; }
}
