using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ModelContextProtocol.Server;
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
    [McpServerTool]
    [Description(
        "Execute C# code in a REPL session. Supports both stateful sessions (with contextId) and single-shot execution (without contextId). For stateful: provide contextId from previous execution to maintain variables and types. For single-shot: omit contextId for one-time execution. Returns contextId for continuing the session. Supports async/await, LINQ, and full .NET 10 API."
    )]
    public static async Task<object> EvaluateCsharp(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        [Description(
            "C# code to execute. Can be expressions, statements, or complete programs. Variables persist in sessions."
        )]
            string code,
        [Description(
            "Optional context ID from previous execution. Omit for single-shot execution, provide to continue a session."
        )]
            string? contextId = null,
        CancellationToken cancellationToken = default
    )
    {
        ScriptState? existingState = null;
        bool isNewContext = false;

        // Handle context
        if (string.IsNullOrWhiteSpace(contextId))
        {
            // Single-shot or new session - create context
            contextId = contextManager.CreateContext();
            isNewContext = true;
        }
        else
        {
            // Continue existing session
            if (!contextManager.ContextExists(contextId))
            {
                return new
                {
                    success = false,
                    error = "REPL_CONTEXT_INVALID",
                    message =
                        $"Context '{contextId}' not found or expired. Omit contextId to create a new session.",
                    suggestedAction = "EvaluateCsharp without contextId",
                    contextId = (string?)null,
                };
            }
            existingState = contextManager.GetContextState(contextId);
        }

        try
        {
            // Execute code
            var result = await scriptingService.ExecuteWithStateAsync(
                code,
                existingState,
                cancellationToken
            );

            // Store state if execution succeeded
            if (result.Success && result.ScriptState != null)
            {
                contextManager.UpdateContextState(contextId, result.ScriptState);
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
                warnings = result.Warnings.Select(w => new
                {
                    code = w.Code,
                    message = w.Message,
                    severity = w.Severity,
                    line = w.Line,
                    column = w.Column,
                }),
                executionTime = result.ExecutionTime.TotalMilliseconds,
                contextId,
            };
        }
        catch
        {
            // Clean up context if it was just created and something went wrong
            if (isNewContext)
            {
                contextManager.RemoveContext(contextId);
            }
            throw;
        }
    }

    /// <summary>
    /// Validate C# code without executing it
    /// </summary>
    [McpServerTool]
    [Description(
        "Validate C# code syntax and semantics WITHOUT executing it. Supports context-aware validation (with contextId) to check against session variables, or context-free validation (without contextId). Returns detailed error/warning information. Fast and safe."
    )]
    public static Task<object> ValidateCsharp(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        [Description(
            "C# code to validate. Checks syntax and semantics without executing."
        )]
            string code,
        [Description(
            "Optional context ID for context-aware validation. Omit for context-free syntax checking."
        )]
            string? contextId = null,
        CancellationToken cancellationToken = default
    )
    {
        ScriptState? existingState = null;

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
                        message =
                            $"Context '{contextId}' not found or expired. Omit contextId for context-free validation.",
                        issues = Array.Empty<object>(),
                    }
                );
            }
            existingState = contextManager.GetContextState(contextId);
        }

        // Validate with or without context
        var script =
            existingState != null
                ? existingState.Script.ContinueWith(code)
                : CSharpScript.Create(code, scriptingService.ScriptOptions);

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
    /// Get information about the REPL environment
    /// </summary>
    [McpServerTool]
    [Description(
        "Get information about REPL environment and active sessions. Shows framework version, capabilities, and session count. Optionally get details about a specific session by providing contextId."
    )]
    public static object GetReplInfo(
        IReplContextManager contextManager,
        [Description("Optional context ID to get session-specific information")]
            string? contextId = null
    )
    {
        var imports = new[]
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Threading.Tasks",
        };

        var baseInfo = new
        {
            frameworkVersion = ".NET 10.0",
            language = "C# 14",
            defaultImports = imports,
            capabilities = new
            {
                asyncAwait = true,
                linq = true,
                topLevelStatements = true,
                consoleOutput = true,
                nugetPackages = true,
                contextManagement = true,
            },
            activeSessions = contextManager.GetActiveContexts().Count,
        };

        // If contextId provided, include session-specific info
        if (!string.IsNullOrWhiteSpace(contextId))
        {
            var metadata = contextManager.GetContextMetadata(contextId);
            if (metadata != null)
            {
                return new
                {
                    baseInfo.frameworkVersion,
                    baseInfo.language,
                    baseInfo.defaultImports,
                    baseInfo.capabilities,
                    baseInfo.activeSessions,
                    session = new
                    {
                        contextId = metadata.ContextId,
                        createdAt = metadata.CreatedAt,
                        lastAccessedAt = metadata.LastAccessedAt,
                        executionCount = metadata.ExecutionCount,
                        isInitialized = metadata.IsInitialized,
                    },
                };
            }

            return new
            {
                baseInfo.frameworkVersion,
                baseInfo.language,
                baseInfo.defaultImports,
                baseInfo.capabilities,
                baseInfo.activeSessions,
                error = $"Session '{contextId}' not found",
            };
        }

        return baseInfo;
    }
}
