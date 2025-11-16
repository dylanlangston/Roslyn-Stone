using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP tools for C# REPL operations
/// </summary>
[McpServerToolType]
public class ReplTools
{
    /// <summary>
    /// Execute C# code in the REPL and return the result
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service for code execution</param>
    /// <param name="code">C# code to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing execution results, including success status, return value, output, errors, warnings, and execution time</returns>
    [McpServerTool]
    [Description(
        "Execute C# code in the REPL and return the result with compilation errors, warnings, and output"
    )]
    public static async Task<object> EvaluateCsharp(
        RoslynScriptingService scriptingService,
        [Description("C# code to execute")] string code,
        CancellationToken cancellationToken = default
    )
    {
        var result = await scriptingService.ExecuteAsync(code, cancellationToken);

        return new
        {
            success = result.Success,
            returnValue = result.ReturnValue,
            output = result.Output,
            errors = result.Errors.Select(e => new
            {
                code = e.Code,
                message = e.Message,
                severity = e.Severity,
                line = e.Line,
                column = e.Column,
            }),
            warnings = result.Warnings.Select(w => new
            {
                code = w.Code,
                message = w.Message,
                severity = w.Severity,
                line = w.Line,
                column = w.Column,
            }),
            executionTime = result.ExecutionTime.TotalMilliseconds,
        };
    }

    /// <summary>
    /// Validate C# code without executing it
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service for code validation</param>
    /// <param name="code">C# code to validate</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An object containing validation results with isValid flag and list of issues</returns>
    [McpServerTool]
    [Description(
        "Validate C# code and return compilation errors and warnings without executing the code"
    )]
    public static async Task<object> ValidateCsharp(
        RoslynScriptingService scriptingService,
        [Description("C# code to validate")] string code,
        CancellationToken cancellationToken = default
    )
    {
        var script = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create(
            code,
            scriptingService.ScriptOptions
        );

        var diagnostics = script.Compile(cancellationToken);

        var issues = new List<object>();
        var errorCount = 0;

        foreach (var diagnostic in diagnostics)
        {
            var severity = diagnostic.Severity;
            if (
                severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error
                || severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning
            )
            {
                if (severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                {
                    errorCount++;
                }

                issues.Add(
                    new
                    {
                        code = diagnostic.Id,
                        message = diagnostic.GetMessage(),
                        severity = severity.ToString(),
                        line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                        column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                    }
                );
            }
        }

        return new { isValid = errorCount == 0, issues = issues };
    }

    /// <summary>
    /// Reset the REPL state
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service to reset</param>
    /// <returns>A confirmation message indicating the REPL state has been reset</returns>
    [McpServerTool]
    [Description("Reset the REPL state, clearing all variables and references")]
    public static string ResetRepl(RoslynScriptingService scriptingService)
    {
        scriptingService.Reset();
        return "REPL state has been reset";
    }
}
