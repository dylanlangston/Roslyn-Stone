namespace RoslynStone.Core.Models;

/// <summary>
/// Represents the result of a package search operation
/// </summary>
public record PackageSearchResult
{
    /// <summary>
    /// Gets the list of found packages
    /// </summary>
    public required IReadOnlyList<PackageMetadata> Packages { get; init; }

    /// <summary>
    /// Gets the total number of packages found
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the search query that was used
    /// </summary>
    public required string Query { get; init; }
}
