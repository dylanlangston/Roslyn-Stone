using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.QueryHandlers;

/// <summary>
/// Handler for getting documentation
/// </summary>
public class GetDocumentationQueryHandler : IQueryHandler<GetDocumentationQuery, DocumentationInfo?>
{
    private readonly DocumentationService _documentationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDocumentationQueryHandler"/> class
    /// </summary>
    /// <param name="documentationService">The documentation service</param>
    public GetDocumentationQueryHandler(DocumentationService documentationService)
    {
        _documentationService = documentationService;
    }

    /// <summary>
    /// Handles the get documentation query
    /// </summary>
    /// <param name="query">The query containing the symbol name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The documentation information or null if not found</returns>
    public Task<DocumentationInfo?> HandleAsync(
        GetDocumentationQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var result = _documentationService.GetDocumentation(query.SymbolName);
        return Task.FromResult(result);
    }
}
