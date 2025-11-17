using Microsoft.CodeAnalysis;
using RoslynStone.Core.Models;

namespace RoslynStone.Infrastructure.Functional;

/// <summary>
/// Pure functional helpers for working with Roslyn diagnostics
/// </summary>
public static class DiagnosticHelpers
{
    /// <summary>
    /// Convert a Roslyn Diagnostic to a CompilationError
    /// </summary>
    public static CompilationError ToCompilationError(this Diagnostic diagnostic) =>
        new()
        {
            Code = diagnostic.Id,
            Message = diagnostic.GetMessage(),
            Severity = diagnostic.Severity.ToString(),
            Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
            Column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
        };

    /// <summary>
    /// Filter diagnostics to only errors and warnings, then convert to CompilationError
    /// </summary>
    public static IReadOnlyList<CompilationError> ToCompilationErrors(
        this IEnumerable<Diagnostic> diagnostics
    ) =>
        diagnostics
            .Where(d =>
                d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning
            )
            .Select(ToCompilationError)
            .ToList();

    /// <summary>
    /// Partition diagnostics into errors and warnings
    /// </summary>
    public static (
        IReadOnlyList<CompilationError> Errors,
        IReadOnlyList<CompilationError> Warnings
    ) PartitionDiagnostics(this IEnumerable<Diagnostic> diagnostics)
    {
        var errors = new List<CompilationError>();
        var warnings = new List<CompilationError>();

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
                errors.Add(diagnostic.ToCompilationError());
            else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                warnings.Add(diagnostic.ToCompilationError());
        }

        return (errors, warnings);
    }

    /// <summary>
    /// Check if any diagnostic is an error
    /// </summary>
    public static bool HasErrors(this IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
}
