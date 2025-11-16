using RoslynStone.Core.CQRS;

namespace RoslynStone.Core.Queries;

/// <summary>
/// Query to get the README content for a NuGet package
/// </summary>
public record GetPackageReadmeQuery(string PackageId, string? Version = null) : IQuery<string?>;
