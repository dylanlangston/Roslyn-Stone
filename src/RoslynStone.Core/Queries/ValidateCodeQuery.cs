using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;

namespace RoslynStone.Core.Queries;

/// <summary>
/// Query to validate C# code without executing it
/// </summary>
public record ValidateCodeQuery(string Code) : IQuery<IReadOnlyList<CompilationError>>;
