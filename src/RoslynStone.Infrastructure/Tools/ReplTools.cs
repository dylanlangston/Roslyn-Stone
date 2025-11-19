using System.ComponentModel;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ModelContextProtocol.Server;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Functional;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP tools for C# REPL operations with context management
/// </summary>
[McpServerToolType]
public class ReplTools
{
    /// <summary>
    /// Execute C# code in a REPL session
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="contextManager">The REPL context manager</param>
    /// <param name="nugetService">The NuGet service for package operations</param>
    /// <param name="code">C# code to execute</param>
    /// <param name="contextId">Optional context ID from previous execution</param>
    /// <param name="nugetPackages">Optional NuGet packages to load before execution</param>
    /// <param name="createContext">Whether to create a persistent context (default: false for single-shot execution)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    [McpServerTool]
    [Description(
        "Execute C# code in a REPL session. Supports both stateful sessions (createContext=true or with contextId) and single-shot execution (createContext=false, default). For stateful: set createContext=true to get contextId for maintaining variables and types across executions. For single-shot: use default createContext=false for temporary execution that is disposed after completion. Can load NuGet packages before execution using nugetPackages parameter. Packages are isolated to the context they are loaded in and are disposed when the context is removed. In stateful contexts, packages persist across executions. Supports async/await, LINQ, and full .NET 10 API."
    )]
    public static async Task<object> EvaluateCsharp(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        NuGetService nugetService,
        [Description(
            "C# code to execute. Can be expressions, statements, or complete programs. Variables persist in stateful sessions."
        )]
            string code,
        [Description(
            "Optional context ID from previous execution. Provide to continue an existing session. When provided, createContext parameter is ignored."
        )]
            string? contextId = null,
        [Description(
            "Optional array of NuGet packages to load before execution. Each package should have 'packageName' (required) and 'version' (optional, uses latest if omitted). Example: [{'packageName': 'Newtonsoft.Json', 'version': '13.0.3'}]. Packages are context-specific and disposed when context is removed."
        )]
            NuGetPackageSpec[]? nugetPackages = null,
        [Description(
            "Whether to create a persistent context. Default is false (single-shot execution with no contextId returned). Set to true to create a stateful session that returns contextId. Ignored when contextId is provided."
        )]
            bool createContext = false,
        CancellationToken cancellationToken = default
    )
    {
        ScriptState? existingState = null;
        ScriptOptions? contextOptions = null;
        bool isNewContext = false;
        bool shouldReturnContextId = false;
        string? activeContextId = null;
        var packageErrors = new List<string>();

        // Handle context logic
        if (!string.IsNullOrWhiteSpace(contextId))
        {
            // Continue existing session (contextId provided)
            if (!contextManager.ContextExists(contextId))
            {
                return new
                {
                    success = false,
                    error = "REPL_CONTEXT_INVALID",
                    message = $"Context '{contextId}' not found or expired. Omit contextId or set createContext=true to create a new session.",
                    suggestedAction = "EvaluateCsharp with createContext=true or without contextId",
                    contextId = (string?)null,
                };
            }
            existingState = contextManager.GetContextState(contextId);
            contextOptions = contextManager.GetContextOptions(contextId);
            activeContextId = contextId;
            shouldReturnContextId = true; // Always return contextId for existing contexts
        }
        else if (createContext)
        {
            // Create new persistent context (createContext=true, no contextId)
            activeContextId = contextManager.CreateContext();
            isNewContext = true;
            shouldReturnContextId = true;
        }
        else
        {
            // Single-shot execution (createContext=false, no contextId)
            // Create temporary context for execution but don't return it
            activeContextId = contextManager.CreateContext();
            isNewContext = true;
            shouldReturnContextId = false;
        }

        try
        {
            // Get base script options (from context or service default)
            var baseOptions = contextOptions ?? scriptingService.ScriptOptions;

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
                        // Load the package
                        var assemblyPaths = await nugetService.DownloadPackageAsync(
                            package.PackageName,
                            package.Version,
                            cancellationToken
                        );

                        // Add assemblies to context-specific options
                        foreach (var assemblyPath in assemblyPaths.Where(File.Exists))
                        {
                            baseOptions = baseOptions.AddReferences(
                                Assembly.LoadFrom(assemblyPath)
                            );
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        packageErrors.Add(
                            $"Failed to load package '{package.PackageName}': {ex.Message}"
                        );
                    }
                }

                // Store updated options in context
                contextManager.UpdateContextOptions(activeContextId!, baseOptions);
            }

            // Execute code with context-specific options
            var result = await scriptingService.ExecuteWithStateAsync(
                code,
                existingState,
                baseOptions,
                cancellationToken
            );

            // Store state if execution succeeded and we're using a context
            if (result.Success && result.ScriptState != null && shouldReturnContextId)
            {
                contextManager.UpdateContextState(activeContextId!, result.ScriptState);
            }

            // Clean up temporary context if single-shot execution
            if (!shouldReturnContextId && activeContextId != null)
            {
                contextManager.RemoveContext(activeContextId);
            }

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
                warnings = result
                    .Warnings.Concat(
                        packageErrors.Select(err => new CompilationError
                        {
                            Code = "PACKAGE_LOAD_ERROR",
                            Message = err,
                            Severity = "Warning",
                        })
                    )
                    .Select(w => new
                    {
                        code = w.Code,
                        message = w.Message,
                        severity = w.Severity,
                        line = w.Line,
                        column = w.Column,
                    }),
                executionTime = result.ExecutionTime.TotalMilliseconds,
                contextId = shouldReturnContextId ? activeContextId : null,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Clean up context if it was just created and something went wrong
            if (isNewContext && activeContextId != null)
            {
                contextManager.RemoveContext(activeContextId);
            }
            throw;
        }
    }

    /// <summary>
    /// Validate C# code without executing it
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="contextManager">The REPL context manager</param>
    /// <param name="code">C# code to validate</param>
    /// <param name="contextId">Optional context ID for context-aware validation</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    [McpServerTool]
    [Description(
        "Validate C# code syntax and semantics WITHOUT executing it. Supports context-aware validation (with contextId) to check against session variables, or context-free validation (without contextId). Returns detailed error/warning information. Fast and safe."
    )]
    public static Task<object> ValidateCsharp(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        [Description("C# code to validate. Checks syntax and semantics without executing.")]
            string code,
        [Description(
            "Optional context ID for context-aware validation. Omit for context-free syntax checking."
        )]
            string? contextId = null,
        CancellationToken cancellationToken = default
    )
    {
        ScriptState? existingState = null;
        ScriptOptions? contextOptions = null;

        // Check for context-aware validation
        if (!string.IsNullOrWhiteSpace(contextId))
        {
            if (!contextManager.ContextExists(contextId))
            {
                return Task.FromResult<object>(
                    new
                    {
                        isValid = false,
                        error = "REPL_CONTEXT_INVALID",
                        message = $"Context '{contextId}' not found or expired. Omit contextId for context-free validation.",
                        issues = Array.Empty<object>(),
                    }
                );
            }
            existingState = contextManager.GetContextState(contextId);
            contextOptions = contextManager.GetContextOptions(contextId);
        }

        // Validate with or without context
        var optionsToUse = contextOptions ?? scriptingService.ScriptOptions;
        var script =
            existingState != null
                ? existingState.Script.ContinueWith(code)
                : CSharpScript.Create(code, optionsToUse);

        var diagnostics = script.Compile(cancellationToken);

        var issues = diagnostics
            .ToCompilationErrors()
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
    /// Reset a REPL session or all sessions
    /// </summary>
    [McpServerTool]
    [Description(
        "Reset REPL session(s), removing variables, types, using directives, and loaded assemblies. Provide contextId to reset a specific session, or omit to reset all sessions. The contextId becomes invalid after reset."
    )]
    public static object ResetRepl(
        IReplContextManager contextManager,
        [Description("Optional context ID to reset. Omit to reset all sessions.")]
            string? contextId = null
    )
    {
        if (string.IsNullOrWhiteSpace(contextId))
        {
            // Reset all contexts
            var activeContexts = contextManager.GetActiveContexts();
            foreach (var id in activeContexts)
            {
                contextManager.RemoveContext(id);
            }

            return new
            {
                success = true,
                message = $"All REPL sessions have been reset ({activeContexts.Count} sessions cleared)",
                sessionsCleared = activeContexts.Count,
            };
        }

        // Reset specific context
        var removed = contextManager.RemoveContext(contextId);

        if (removed)
        {
            return new
            {
                success = true,
                message = $"REPL session '{contextId}' has been reset",
                contextId,
            };
        }

        return new
        {
            success = false,
            message = $"Context '{contextId}' not found or already removed",
            contextId,
        };
    }
}
