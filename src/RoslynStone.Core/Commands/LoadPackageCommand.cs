using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;

namespace RoslynStone.Core.Commands;

/// <summary>
/// Command to load a NuGet package
/// </summary>
public record LoadPackageCommand(string PackageName, string? Version = null) : ICommand<PackageReference>;
