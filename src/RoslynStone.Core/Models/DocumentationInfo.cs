namespace RoslynStone.Core.Models;

/// <summary>
/// XML documentation information for a symbol
/// </summary>
public record DocumentationInfo
{
    /// <summary>
    /// Gets the symbol name
    /// </summary>
    public required string SymbolName { get; init; }

    /// <summary>
    /// Gets the summary documentation
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Gets the remarks documentation
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// Gets the parameter documentation
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the return value documentation
    /// </summary>
    public string? Returns { get; init; }

    /// <summary>
    /// Gets the exception documentation
    /// </summary>
    public IReadOnlyList<string> Exceptions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the example documentation
    /// </summary>
    public string? Example { get; init; }

    /// <summary>
    /// Gets the full XML documentation
    /// </summary>
    public string? FullDocumentation { get; init; }
}
