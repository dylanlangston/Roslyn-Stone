namespace RoslynStone.Core.Models;

/// <summary>
/// Represents the result of a package search operation
/// </summary>
public class PackageSearchResult
{
    /// <summary>
    /// Gets or sets the list of found packages
    /// </summary>
    public List<PackageMetadata> Packages { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of packages found
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the search query that was used
    /// </summary>
    public string Query { get; set; } = string.Empty;
}
