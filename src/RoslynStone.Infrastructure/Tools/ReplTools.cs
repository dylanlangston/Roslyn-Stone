using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ModelContextProtocol.Server;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Functional;
using RoslynStone.Infrastructure.Helpers;
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
    /// <remarks>
    /// SECURITY CONSIDERATION: This tool loads NuGet packages based on user input without validation of package sources or integrity checks.
    /// In production deployments, consider implementing additional security measures such as:
    /// - Restricting package sources to trusted feeds only
    /// - Implementing package signature verification
    /// - Requiring package approval workflows
    /// - Using a package allow-list for sensitive environments
    /// </remarks>
    [McpServerTool]
    [Description(
        "Execute C# code to create and test single-file utility programs (file-based C# apps using top-level statements). Ideal for building small utilities, scripts, and tools in a single .cs file. Supports both stateful sessions (createContext=true or with contextId) for iterative development and single-shot execution (createContext=false, default) for testing complete programs. Can load NuGet packages before execution using nugetPackages parameter. Supports async/await, LINQ, and full .NET 10 API. Use this to develop file-based C# apps that can be run with 'dotnet run app.cs'. For final self-contained apps, use the #:package directive in your .cs file instead of nugetPackages parameter (e.g., '#:package Newtonsoft.Json@13.0.3' at the top of the file)."
    )]
    public static async Task<object> EvaluateCsharp(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        NuGetService nugetService,
        [Description(
            "C# code to execute. Use top-level statements to create single-file utility programs. Can be expressions, statements, or complete programs. Variables persist in stateful sessions for iterative development."
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
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ScriptState? existingState = null;
        ScriptOptions? contextOptions = null;
        bool isNewContext = false;
        bool shouldReturnContextId;
        string activeContextId;
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
                    errors = new[]
                    {
                        new
                        {
                            code = "CONTEXT_NOT_FOUND",
                            message = $"Context '{contextId}' not found or expired. Create a new context with createContext: true, or omit contextId to start fresh.",
                            severity = "Error",
                        },
                    },
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
            bool packagesAdded = false;

            // Load NuGet packages if provided
            if (nugetPackages != null && nugetPackages.Length > 0)
            {
                // If trying to add packages to an existing context with state, warn that variables will be lost
                if (existingState != null && !string.IsNullOrWhiteSpace(contextId))
                {
                    packageErrors.Add(
                        "Warning: Adding packages to an existing context resets the session. All previously defined variables, types, and state will be lost due to Roslyn Scripting API limitations (ScriptState.ContinueWithAsync doesn't accept new options). For best results, specify packages when creating the context (createContext=true with nugetPackages at the same time)."
                    );
                }

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

                        // Add assemblies to context-specific options using MetadataReference
                        // This avoids loading assemblies into the runtime (Assembly.LoadFrom)
                        foreach (var assemblyPath in assemblyPaths.Where(File.Exists))
                        {
                            baseOptions = baseOptions.AddReferences(
                                MetadataReference.CreateFromFile(assemblyPath)
                            );
                            packagesAdded = true;
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
                contextManager.UpdateContextOptions(activeContextId, baseOptions);

                // CRITICAL: Reset script state when packages are added
                // ScriptState.ContinueWithAsync() doesn't accept new options,
                // so we must start fresh to use the updated options with new packages
                // This means all variables/state will be lost, which is why we warn above
                if (packagesAdded && existingState != null)
                {
                    existingState = null;
                }
            }
            // If this is a new context and no packages were loaded, store the base options
            else if (isNewContext)
            {
                contextManager.UpdateContextOptions(activeContextId, baseOptions);
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
                contextManager.UpdateContextState(activeContextId, result.ScriptState);
            }

            // Clean up temporary context if single-shot execution
            if (!shouldReturnContextId)
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
            if (isNewContext)
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
        "Validate C# code syntax and semantics WITHOUT executing it. Use this to check single-file utility programs before execution. Supports context-aware validation (with contextId) to check against session variables, or context-free validation (without contextId). Returns detailed error/warning information. Fast and safe."
    )]
    public static Task<object> ValidateCsharp(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        [Description(
            "C# code to validate. Use top-level statements for single-file utility programs. Checks syntax and semantics without executing."
        )]
            string code,
        [Description(
            "Optional context ID for context-aware validation. Omit for context-free syntax checking."
        )]
            string? contextId = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        // Strip file-based program directives (#:package, #:sdk, etc.) as they are build-time directives
        var processedCode = MetadataReferenceHelper.StripFileBasedProgramDirectives(code);

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
                ? existingState.Script.ContinueWith(processedCode)
                : CSharpScript.Create(processedCode, optionsToUse);

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

    /// <summary>
    /// Get current REPL environment information
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="contextManager">The REPL context manager</param>
    /// <param name="contextId">Optional context ID for session-specific information</param>
    /// <returns>Information about current REPL state and capabilities</returns>
    [McpServerTool]
    [Description(
        "Access current execution environment state and capabilities. Returns information about framework version, available namespaces, loaded assemblies, imported NuGet packages, and capabilities for building single-file C# utility programs. Optionally provide contextId for session-specific state information."
    )]
    public static object GetReplInfo(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        [Description(
            "Optional context ID for session-specific state. Omit for general REPL information."
        )]
            string? contextId = null
    )
    {
        var imports = new List<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Threading.Tasks",
        };

        var activeSessionCount = contextManager.GetActiveContexts().Count;

        var tips = new List<string>
        {
            "Create single-file utility programs using top-level statements",
            "Use 'using' directives at the top to import namespaces",
            "Console.WriteLine output is captured separately from return values",
            "Async/await is fully supported for async operations",
            "Use nugetPackages parameter to load external libraries inline during testing",
            "Use LoadNuGetPackage or SearchNuGetPackages to discover and add libraries",
            "Use ValidateCsharp to check your utility program before execution",
            "Use GetDocumentation to learn about .NET APIs",
            "Build complete, runnable .cs files that work with 'dotnet run app.cs'",
            "For final self-contained apps, use #:package directive instead of nugetPackages",
        };

        var capabilities = new
        {
            asyncAwait = true,
            linq = true,
            topLevelStatements = true,
            consoleOutput = true,
            nugetPackages = true,
            statefulness = true,
        };

        var examples = new
        {
            helloWorld = "Console.WriteLine(\"Hello, World!\");",
            simpleUtility = "var args = Environment.GetCommandLineArgs(); Console.WriteLine($\"Args: {string.Join(\", \", args)}\");",
            asyncOperation = "await Task.Delay(100); Console.WriteLine(\"Done\");",
            linqQuery = "var numbers = new[] { 1, 2, 3, 4, 5 }; var doubled = numbers.Select(x => x * 2); Console.WriteLine(string.Join(\", \", doubled));",
            fileBasedApp = "// Single-file utility program\nusing System.IO;\nvar files = Directory.GetFiles(\".\");\nforeach (var file in files) Console.WriteLine(Path.GetFileName(file));",
            withPackages = "// Testing with package loading\n// EvaluateCsharp with nugetPackages: [{packageName: 'Humanizer', version: '3.0.1'}]\nusing Humanizer;\n\"test\".Humanize()",
        };

        object? sessionMetadata = null;
        if (!string.IsNullOrWhiteSpace(contextId))
        {
            var metadata = contextManager.GetContextMetadata(contextId);
            if (metadata != null)
            {
                sessionMetadata = new
                {
                    contextId = metadata.ContextId,
                    createdAt = metadata.CreatedAt,
                    lastAccessedAt = metadata.LastAccessedAt,
                    executionCount = metadata.ExecutionCount,
                    isInitialized = metadata.IsInitialized,
                };
            }
        }

        return new
        {
            frameworkVersion = RuntimeInformation.FrameworkDescription,
            language = "C# 14",
            state = "Ready",
            activeSessionCount,
            contextId,
            isSessionSpecific = !string.IsNullOrWhiteSpace(contextId),
            defaultImports = imports,
            capabilities,
            tips,
            examples,
            sessionMetadata,
        };
    }
}
