namespace RoslynStone.Core.Models;

/// <summary>
/// XML documentation information for a symbol
/// </summary>
public class DocumentationInfo
{
    /// <summary>
    /// Gets or sets the symbol name
    /// </summary>
    public string SymbolName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the summary documentation
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Gets or sets the remarks documentation
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// Gets or sets the parameter documentation
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>
    /// Gets or sets the return value documentation
    /// </summary>
    public string? Returns { get; init; }

    /// <summary>
    /// Gets or sets the exception documentation
    /// </summary>
    public List<string> Exceptions { get; init; } = new();

    /// <summary>
    /// Gets or sets the example documentation
    /// </summary>
    public string? Example { get; init; }

    /// <summary>
    /// Gets or sets the full XML documentation
    /// </summary>
    public string? FullDocumentation { get; set; }
}
