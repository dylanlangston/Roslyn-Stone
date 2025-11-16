using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;

namespace RoslynStone.Core.Queries;

/// <summary>
/// Query to search for NuGet packages
/// </summary>
public record SearchPackagesQuery(string Query, int Skip = 0, int Take = 20)
    : IQuery<PackageSearchResult>;
