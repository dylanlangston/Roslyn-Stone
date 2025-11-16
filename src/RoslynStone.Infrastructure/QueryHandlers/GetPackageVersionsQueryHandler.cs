using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.QueryHandlers;

/// <summary>
/// Handler for getting package versions
/// </summary>
public class GetPackageVersionsQueryHandler
    : IQueryHandler<GetPackageVersionsQuery, List<PackageVersion>>
{
    private readonly NuGetService _nugetService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPackageVersionsQueryHandler"/> class
    /// </summary>
    /// <param name="nugetService">The NuGet service</param>
    public GetPackageVersionsQueryHandler(NuGetService nugetService)
    {
        _nugetService = nugetService;
    }

    /// <summary>
    /// Handles the get package versions query
    /// </summary>
    /// <param name="query">The query containing the package ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of package versions</returns>
    public async Task<List<PackageVersion>> HandleAsync(
        GetPackageVersionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return await _nugetService.GetPackageVersionsAsync(query.PackageId, cancellationToken);
    }
}
