using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.QueryHandlers;

/// <summary>
/// Handler for searching NuGet packages
/// </summary>
public class SearchPackagesQueryHandler : IQueryHandler<SearchPackagesQuery, PackageSearchResult>
{
    private readonly NuGetService _nugetService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchPackagesQueryHandler"/> class
    /// </summary>
    /// <param name="nugetService">The NuGet service</param>
    public SearchPackagesQueryHandler(NuGetService nugetService)
    {
        _nugetService = nugetService;
    }

    /// <summary>
    /// Handles the search packages query
    /// </summary>
    /// <param name="query">The query containing search parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The search results</returns>
    public async Task<PackageSearchResult> HandleAsync(
        SearchPackagesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return await _nugetService.SearchPackagesAsync(
            query.Query,
            query.Skip,
            query.Take,
            cancellationToken
        );
    }
}
