using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;

namespace RoslynStone.Infrastructure.QueryHandlers;

/// <summary>
/// Handler for validating C# code
/// </summary>
public class ValidateCodeQueryHandler
    : IQueryHandler<ValidateCodeQuery, IReadOnlyList<CompilationError>>
{
    public Task<IReadOnlyList<CompilationError>> HandleAsync(
        ValidateCodeQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<CompilationError>();

        try
        {
            var script = CSharpScript.Create(query.Code, ScriptOptions.Default);
            var diagnostics = script.Compile(cancellationToken);

            foreach (var diagnostic in diagnostics)
            {
                var error = new CompilationError
                {
                    Code = diagnostic.Id,
                    Message = diagnostic.GetMessage(),
                    Severity = diagnostic.Severity.ToString(),
                    Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                    Column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                };

                if (
                    diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error
                    || diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning
                )
                {
                    errors.Add(error);
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(
                new CompilationError
                {
                    Code = "VALIDATION_ERROR",
                    Message = ex.Message,
                    Severity = "Error",
                }
            );
        }

        return Task.FromResult<IReadOnlyList<CompilationError>>(errors);
    }
}
