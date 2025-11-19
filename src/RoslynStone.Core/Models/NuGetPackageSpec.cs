namespace RoslynStone.Core.Models;

/// <summary>
/// Specification for loading a NuGet package in a REPL context
/// </summary>
public record NuGetPackageSpec
{
    /// <summary>
    /// Gets the package name (required)
    /// </summary>
    public required string PackageName { get; init; }

    /// <summary>
    /// Gets the package version (optional, uses latest stable if not specified)
    /// </summary>
    public string? Version { get; init; }
}
