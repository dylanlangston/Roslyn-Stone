using RoslynStone.Core.CQRS;
using RoslynStone.Core.Queries;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.QueryHandlers;

/// <summary>
/// Handler for getting package README
/// </summary>
public class GetPackageReadmeQueryHandler : IQueryHandler<GetPackageReadmeQuery, string?>
{
    private readonly NuGetService _nugetService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPackageReadmeQueryHandler"/> class
    /// </summary>
    /// <param name="nugetService">The NuGet service</param>
    public GetPackageReadmeQueryHandler(NuGetService nugetService)
    {
        _nugetService = nugetService;
    }

    /// <summary>
    /// Handles the get package README query
    /// </summary>
    /// <param name="query">The query containing package ID and optional version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>README content or null if not found</returns>
    public async Task<string?> HandleAsync(
        GetPackageReadmeQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return await _nugetService.GetPackageReadmeAsync(
            query.PackageId,
            query.Version,
            cancellationToken
        );
    }
}
