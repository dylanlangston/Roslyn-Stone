namespace RoslynStone.Core.Models;

/// <summary>
/// XML documentation information for a symbol
/// </summary>
public class DocumentationInfo
{
    public string SymbolName { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Remarks { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string? Returns { get; set; }
    public List<string> Exceptions { get; set; } = new();
    public string? Example { get; set; }
    public string? FullDocumentation { get; set; }
}
