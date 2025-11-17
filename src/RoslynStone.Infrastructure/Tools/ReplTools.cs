using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Functional;
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
        "Execute C# code in a stateful REPL and return results. Use this to run C# expressions, statements, or complete programs. The REPL maintains state between calls - variables, types, and using directives persist across executions. Supports async/await, LINQ, and full .NET 10 API. Returns success status, return value, Console output, compilation errors/warnings, and execution time. Perfect for: testing code snippets, iterative development, data processing, algorithm experimentation, and learning C# interactively."
    )]
    public static async Task<object> EvaluateCsharp(
        RoslynScriptingService scriptingService,
        [Description(
            "C# code to execute. Can be a single expression (e.g., '2 + 2'), multiple statements (e.g., 'var x = 10; x * 2'), or complete programs with classes and methods. Use 'return' for explicit returns. Variables and types persist between calls. Supports top-level statements, async/await, and LINQ. Console.WriteLine output is captured separately from return values."
        )]
            string code,
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
        "Validate C# code syntax and semantics WITHOUT executing it. Use this before EvaluateCsharp to catch compilation errors safely. This is fast, safe, and doesn't change REPL state. Returns detailed error/warning information with line numbers, column numbers, error codes, and helpful messages. Perfect for: checking syntax before execution, understanding compilation errors, learning correct C# syntax, and validating complex code structures."
    )]
    public static Task<object> ValidateCsharp(
        RoslynScriptingService scriptingService,
        [Description(
            "C# code to validate. Checks syntax and semantics without executing. Use this to catch errors like missing semicolons, type mismatches, undefined variables, and invalid operations before running the code."
        )]
            string code,
        CancellationToken cancellationToken = default
    )
    {
        var script = CSharpScript.Create(code, scriptingService.ScriptOptions);
        var diagnostics = script.Compile(cancellationToken);
        
        var issues = diagnostics.ToCompilationErrors()
            .Select(e => new
            {
                code = e.Code,
                message = e.Message,
                severity = e.Severity,
                line = e.Line,
                column = e.Column,
            })
            .ToList();

        return Task.FromResult<object>(new { isValid = !diagnostics.HasErrors(), issues });
    }

    /// <summary>
    /// Reset the REPL state
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service to reset</param>
    /// <returns>A confirmation message indicating the REPL state has been reset</returns>
    [McpServerTool]
    [Description(
        "Reset the REPL to a clean state, removing all variables, types, using directives, and loaded assemblies. Use this when: starting a new experiment, clearing conflicting definitions, recovering from errors, or wanting a fresh environment. After reset, you'll need to re-load any NuGet packages and re-define any variables. This is a clean slate operation."
    )]
    public static string ResetRepl(RoslynScriptingService scriptingService)
    {
        scriptingService.Reset();
        return "REPL state has been reset";
    }

    /// <summary>
    /// Get information about the current REPL environment
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <returns>Information about current REPL state including available namespaces and capabilities</returns>
    [McpServerTool]
    [Description(
        "Get comprehensive information about the current REPL environment and capabilities. Shows available namespaces, framework version, supported features, and helpful tips. Use this to: understand what's available by default, learn about REPL capabilities, see which namespaces are imported, and get oriented in a new session. Perfect for getting started or checking the current state."
    )]
    public static object GetReplInfo(RoslynScriptingService scriptingService)
    {
        var imports = new[]
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Threading.Tasks",
        };

        return new
        {
            frameworkVersion = ".NET 10.0",
            language = "C# 14",
            state = "Ready",
            defaultImports = imports,
            capabilities = new
            {
                asyncAwait = true,
                linq = true,
                topLevelStatements = true,
                consoleOutput = true,
                nugetPackages = true,
                statefulness = true,
            },
            tips = new[]
            {
                "Variables and types persist between executions",
                "Use 'using' directives to import additional namespaces",
                "Console.WriteLine output is captured separately from return values",
                "Async/await is fully supported in the REPL",
                "Use LoadNuGetPackage to add external libraries",
                "Use ResetRepl to clear all state and start fresh",
                "Use ValidateCsharp to check syntax before execution",
                "Use GetDocumentation to learn about .NET APIs",
            },
            examples = new
            {
                simpleExpression = "2 + 2",
                variableDeclaration = "var name = \"Alice\"; name",
                asyncOperation = "await Task.Delay(100); \"Done\"",
                linqQuery = "new[] { 1, 2, 3 }.Select(x => x * 2)",
                consoleOutput = "Console.WriteLine(\"Debug\"); return \"Result\"",
            },
        };
    }
}
