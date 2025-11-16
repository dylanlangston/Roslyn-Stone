namespace RoslynStone.Core.Models;

/// <summary>
/// Represents a NuGet package reference
/// </summary>
public class PackageReference
{
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public bool IsLoaded { get; set; }
}
