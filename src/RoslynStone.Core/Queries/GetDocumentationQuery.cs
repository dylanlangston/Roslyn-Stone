using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;

namespace RoslynStone.Core.Queries;

/// <summary>
/// Query to get documentation for a symbol
/// </summary>
public record GetDocumentationQuery(string SymbolName) : IQuery<DocumentationInfo?>;
