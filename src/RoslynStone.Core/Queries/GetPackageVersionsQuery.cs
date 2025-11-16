using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;

namespace RoslynStone.Core.Queries;

/// <summary>
/// Query to get all versions of a NuGet package
/// </summary>
public record GetPackageVersionsQuery(string PackageId) : IQuery<List<PackageVersion>>;
