using Microsoft.AspNetCore.Mvc;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;

namespace RoslynStone.Api.Controllers;

/// <summary>
/// Documentation controller for looking up XML documentation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentationController : ControllerBase
{
    private readonly IQueryHandler<GetDocumentationQuery, DocumentationInfo?> _handler;

    public DocumentationController(IQueryHandler<GetDocumentationQuery, DocumentationInfo?> handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Get documentation for a symbol
    /// </summary>
    [HttpGet("{symbolName}")]
    public async Task<ActionResult<DocumentationInfo>> GetDocumentation(string symbolName)
    {
        var query = new GetDocumentationQuery(symbolName);
        var result = await _handler.HandleAsync(query);

        if (result == null)
            return NotFound($"Documentation not found for symbol: {symbolName}");

        return Ok(result);
    }
}
