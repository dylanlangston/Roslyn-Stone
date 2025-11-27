using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModelContextProtocol.Server;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Functional;
using RoslynStone.Infrastructure.Helpers;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP tools for file-based C# execution with isolated context management
/// </summary>
[McpServerToolType]
public class FileBasedTools
{
    /// <summary>
    /// Execute C# code to test file-based C# apps
    /// </summary>
    /// <param name="isolatedExecutionService">The isolated execution service for all executions</param>
    /// <param name="contextManager">The REPL context manager for tracking metadata</param>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="securityConfig">Security configuration for context ID masking</param>
    /// <param name="compilationService">The compilation service (unused, kept for compatibility)</param>
    /// <param name="code">C# code to execute</param>
    /// <param name="contextId">Optional context ID for tracking metadata</param>
    /// <param name="nugetPackages">Optional NuGet packages to load before execution</param>
    /// <param name="createContext">Whether to create a persistent context (default: false for single-shot execution)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <remarks>
    /// ARCHITECTURE: Uses SessionIsolatedExecutionService for ALL executions.
    /// File-based C# apps are single-shot programs that run once and complete.
    /// Each execution compiles code, loads into UnloadableAssemblyLoadContext, executes, then unloads.
    /// Proper memory cleanup and security isolation for all scenarios.
    ///
    /// NOTE: This aligns with the file-based app model (dotnet run app.cs) - programs are independent
    /// executions, not stateful REPL sessions. Variables do NOT persist between calls.
    /// </remarks>
    [McpServerTool]
    [Description(
        "Execute C# code as isolated, single-shot programs (file-based app model with top-level statements). Each execution runs independently - variables/state do NOT persist between calls. Perfect for testing utility programs before saving as .cs files. \n\nSUPPORTS: Top-level await, LINQ, async/await, full .NET 10 API, console output capture. \n\nEXAMPLES:\n- Simple: return 2 + 2;\n- Variables: var x = 10; return x * 2;\n- LINQ: return Enumerable.Range(1, 100).Where(x => x % 2 == 0).Sum();\n- Async: var response = await new HttpClient().GetStringAsync(\"https://api.github.com\"); return response.Length;\n- Console: Console.WriteLine(\"Hello\"); return 42;\n\nNUGET: Load packages for testing before adding #:package directives to final .cs file. Use nugetPackages parameter."
    )]
    public static async Task<object> EvaluateCsharp(
        SessionIsolatedExecutionService isolatedExecutionService,
        IExecutionContextManager contextManager,
        NuGetService nugetService,
        Models.SecurityConfiguration securityConfig,
        CompilationService compilationService,
        [Description(
            "C# code to execute using top-level statements. Supports: expressions (2+2), variable declarations (var x=10; return x*2;), async/await, LINQ, console output. Each execution is isolated - NO state persistence between calls."
        )]
            string code,
        [Description(
            "Optional context ID for tracking execution history metadata. Does NOT provide state persistence - each execution is isolated. When provided, createContext parameter is ignored."
        )]
            string? contextId = null,
        [Description(
            "Optional array of NuGet packages to load before execution. Each package should have 'packageName' (required) and 'version' (optional, uses latest if omitted). Example: [{'packageName': 'Newtonsoft.Json', 'version': '13.0.3'}]. Test packages here before adding #:package directives to your final .cs file."
        )]
            NuGetPackageSpec[]? nugetPackages = null,
        [Description(
            "Whether to create a context for tracking metadata. Default is false. Set to true to get contextId for execution tracking. Note: Does NOT enable state persistence - all executions are isolated."
        )]
            bool createContext = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        bool hasPackages = nugetPackages != null && nugetPackages.Length > 0;

        // ALL EXECUTIONS use SessionIsolatedExecutionService for proper cleanup
        // This aligns with file-based app model: single-shot, independent executions
        return await ExecuteWithIsolationAsync(
            isolatedExecutionService,
            contextManager,
            nugetService,
            securityConfig,
            code,
            contextId,
            nugetPackages,
            createContext,
            cancellationToken
        );
    }

    /// <summary>
    /// Execute code using SessionIsolatedExecutionService for proper isolation.
    /// All executions are isolated and independent, aligned with file-based app model.
    /// </summary>
    private static async Task<object> ExecuteWithIsolationAsync(
        SessionIsolatedExecutionService isolatedExecutionService,
        IExecutionContextManager contextManager,
        NuGetService nugetService,
        Models.SecurityConfiguration securityConfig,
        string code,
        string? contextId,
        NuGetPackageSpec[]? nugetPackages,
        bool createContext,
        CancellationToken cancellationToken
    )
    {
        var packageErrors = new List<string>();
        var metadataReferences = new List<MetadataReference>();

        // Load NuGet packages if provided
        if (nugetPackages != null && nugetPackages.Length > 0)
        {
            foreach (var package in nugetPackages)
            {
                if (string.IsNullOrWhiteSpace(package.PackageName))
                {
                    packageErrors.Add("Package name is null or empty");
                    continue;
                }

                try
                {
                    var assemblyPaths = await nugetService.DownloadPackageAsync(
                        package.PackageName,
                        package.Version,
                        cancellationToken
                    );

                    foreach (var assemblyPath in assemblyPaths.Where(File.Exists))
                    {
                        metadataReferences.Add(MetadataReference.CreateFromFile(assemblyPath));
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    packageErrors.Add(
                        $"Failed to load package '{package.PackageName}': {ex.Message}"
                    );
                }
            }
        }

        // Determine context ID (existing, new persistent, or temporary)
        string activeContextId;
        bool shouldReturnContextId;

        if (!string.IsNullOrWhiteSpace(contextId))
        {
            // Use provided context ID and register in context manager for tracking
            activeContextId = contextId;
            shouldReturnContextId = true;

            // Create context if it doesn't exist (for metadata tracking)
            if (!contextManager.ContextExists(contextId))
            {
                contextManager.CreateContext(); // Creates with new ID but we track the provided one
            }
        }
        else if (createContext)
        {
            // Create new context for tracking
            activeContextId = contextManager.CreateContext();
            shouldReturnContextId = true;
        }
        else
        {
            // Temporary context, not tracked
            activeContextId = Guid.NewGuid().ToString();
            shouldReturnContextId = false;
        }

        try
        {
            // Execute in isolated context
            var result = await isolatedExecutionService.ExecuteInContextAsync(
                activeContextId,
                code,
                metadataReferences,
                cancellationToken
            );

            // Always clean up isolated context after execution (file-based app model)
            await isolatedExecutionService.UnloadContextAsync(activeContextId);

            // Mask context ID for security
            var maskedContextId = shouldReturnContextId
                ? ContextIdMasker.Mask(activeContextId, !securityConfig.LogContextIds)
                : null;

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
                    line = (int?)e.Line,
                    column = (int?)e.Column,
                }),
                warnings = packageErrors
                    .Select(err => new
                    {
                        code = "PACKAGE_LOAD_ERROR",
                        message = err,
                        severity = "Warning",
                        line = (int?)null,
                        column = (int?)null,
                    })
                    .Concat(
                        result.Warnings.Select(w => new
                        {
                            code = w.Code,
                            message = w.Message,
                            severity = w.Severity,
                            line = (int?)w.Line,
                            column = (int?)w.Column,
                        })
                    ),
                executionTime = result.ExecutionTime.TotalMilliseconds,
                contextId = maskedContextId,
                isolatedExecution = true,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Clean up on error
            await isolatedExecutionService.UnloadContextAsync(activeContextId);
            throw;
        }
    }

    /// <summary>
    /// Validate C# code without executing it
    /// </summary>
    /// <param name="compilationService">The compilation service</param>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="securityConfig">Security configuration for context ID masking</param>
    /// <param name="code">C# code to validate</param>
    /// <param name="nugetPackages">Optional NuGet packages to load before validation</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    [McpServerTool]
    [Description(
        "Validate C# code syntax and semantics WITHOUT executing it (compilation check only). Fast and safe way to check code correctness before execution. \n\nUSE CASES:\n- Check syntax errors before execution\n- Verify types/methods exist\n- Validate NuGet package usage\n- Test code snippets safely\n\nEXAMPLES:\n- Basic: var x = 10;\n- With types: List<string> items = new();\n- LINQ: Enumerable.Range(1, 10).Select(x => x * 2)\n- Async: await Task.Delay(100); return 42;\n\nNUGET: Use nugetPackages parameter to validate code using external packages (checks if types are available)."
    )]
    public static async Task<object> ValidateCsharp(
        CompilationService compilationService,
        NuGetService nugetService,
        Models.SecurityConfiguration securityConfig,
        [Description(
            "C# code to validate using top-level statements. Checks: syntax correctness, type resolution, method signatures, NuGet package types. Does NOT execute code."
        )]
            string code,
        [Description(
            "Optional array of NuGet packages to load before validation. Each package should have 'packageName' (required) and 'version' (optional, uses latest if omitted). Example: [{'packageName': 'Newtonsoft.Json', 'version': '13.0.3'}]. Allows validation of code using external package types."
        )]
            NuGetPackageSpec[]? nugetPackages = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await ValidateWithCompilationServiceAsync(
            compilationService,
            nugetService,
            code,
            nugetPackages,
            cancellationToken
        );
    }

    /// <summary>
    /// Validate code using CompilationService
    /// </summary>
    private static async Task<object> ValidateWithCompilationServiceAsync(
        CompilationService compilationService,
        NuGetService nugetService,
        string code,
        NuGetPackageSpec[]? nugetPackages,
        CancellationToken cancellationToken
    )
    {
        {
            var packageErrors = new List<string>();
            var metadataReferences = new List<MetadataReference>();

            // Load NuGet packages if provided
            if (nugetPackages != null && nugetPackages.Length > 0)
            {
                foreach (var package in nugetPackages)
                {
                    if (string.IsNullOrWhiteSpace(package.PackageName))
                    {
                        packageErrors.Add("Package name is null or empty");
                        continue;
                    }

                    try
                    {
                        var assemblyPaths = await nugetService.DownloadPackageAsync(
                            package.PackageName,
                            package.Version,
                            cancellationToken
                        );

                        foreach (var assemblyPath in assemblyPaths.Where(File.Exists))
                        {
                            metadataReferences.Add(MetadataReference.CreateFromFile(assemblyPath));
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        packageErrors.Add(
                            $"Failed to load package '{package.PackageName}': {ex.Message}"
                        );
                    }
                }
            }

            // Compile with packages (don't execute, just validate)
            // Parse as Script to accept REPL-style syntax (like implicit returns, trailing expressions)
            var parseOptions = new CSharpParseOptions(
                kind: SourceCodeKind.Script,
                languageVersion: LanguageVersion.Preview
            );
            var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);

            // Get default references
            var defaultOptions = MetadataReferenceHelper.GetDefaultScriptOptions();
            var references = defaultOptions
                .MetadataReferences.OfType<PortableExecutableReference>()
                .Concat(metadataReferences.OfType<PortableExecutableReference>())
                .ToList();

            var compilation = CSharpCompilation.Create(
                $"ValidationAssembly_{Guid.NewGuid():N}",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(
                    OutputKind.ConsoleApplication,
                    optimizationLevel: OptimizationLevel.Release,
                    allowUnsafe: false
                )
            );

            var diagnostics = compilation.GetDiagnostics(cancellationToken);

            var issues = diagnostics
                .Where(d =>
                    d.Severity == DiagnosticSeverity.Error
                    || d.Severity == DiagnosticSeverity.Warning
                )
                .Select(d => new
                {
                    code = d.Id,
                    message = d.GetMessage(),
                    severity = d.Severity.ToString(),
                    line = (int?)d.Location.GetLineSpan().StartLinePosition.Line,
                    column = (int?)d.Location.GetLineSpan().StartLinePosition.Character,
                })
                .Concat(
                    packageErrors.Select(err => new
                    {
                        code = "PACKAGE_LOAD_ERROR",
                        message = err,
                        severity = "Warning",
                        line = (int?)null,
                        column = (int?)null,
                    })
                )
                .ToList();

            var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

            return new { isValid = !hasErrors, issues };
        }
    }
}
