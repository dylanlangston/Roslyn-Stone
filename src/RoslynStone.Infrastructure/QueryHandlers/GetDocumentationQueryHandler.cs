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

    public GetDocumentationQueryHandler(DocumentationService documentationService)
    {
        _documentationService = documentationService;
    }

    public Task<DocumentationInfo?> HandleAsync(GetDocumentationQuery query, CancellationToken cancellationToken = default)
    {
        var result = _documentationService.GetDocumentation(query.SymbolName);
        return Task.FromResult(result);
    }
}
