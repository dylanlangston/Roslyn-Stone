using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Resources;

/// <summary>
/// MCP resource for REPL state information
/// </summary>
[McpServerResourceType]
public class ReplStateResource
{
    /// <summary>
    /// Get current REPL environment information as a resource
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="contextManager">The REPL context manager</param>
    /// <param name="uri">The resource URI: repl://state, repl://info, or repl://sessions/{contextId}/state</param>
    /// <returns>Information about current REPL state</returns>
    [McpServerResource]
    [Description(
        "Access current REPL environment state and capabilities. Returns information about framework version, available namespaces, loaded assemblies, imported NuGet packages, current variables in scope, and REPL capabilities. Use this to understand what's available, check the current state, and get oriented in a session. For session-specific state, use 'repl://sessions/{contextId}/state'."
    )]
    public static object GetReplState(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        [Description(
            "Resource URI. Use 'repl://state' or 'repl://info' for general info, or 'repl://sessions/{contextId}/state' for session-specific state."
        )]
            string uri
    )
    {
        // Check if this is a session-specific state request
        var isSessionSpecific = uri.Contains("/sessions/", StringComparison.OrdinalIgnoreCase);
        string? contextId = null;

        if (isSessionSpecific)
        {
            // Extract context ID from URI: repl://sessions/{contextId}/state
            var parts = uri.Split('/');
            var sessionIndex = Array.FindIndex(
                parts,
                p => p.Equals("sessions", StringComparison.OrdinalIgnoreCase)
            );
            if (sessionIndex >= 0 && sessionIndex + 1 < parts.Length)
            {
                contextId = parts[sessionIndex + 1];
            }
        }

        var imports = new[]
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Threading.Tasks",
        };

        // Get session information from context manager
        var activeSessionCount = contextManager.GetActiveContexts().Count;

        var baseResponse = new
        {
            uri,
            mimeType = "application/json",
            frameworkVersion = ".NET 10.0",
            language = "C# 14",
            state = "Ready",
            activeSessionCount,
            contextId = contextId, // Will be null for general queries, populated for session-specific
            isSessionSpecific = isSessionSpecific,
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
                "Use documentation resources to learn about .NET APIs",
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

        // If session-specific state is requested, include session metadata
        return (isSessionSpecific && !string.IsNullOrEmpty(contextId))
            ? (
                contextManager.GetContextMetadata(contextId) is var metadata && metadata != null
                    ? new
                    {
                        baseResponse.uri,
                        baseResponse.mimeType,
                        baseResponse.frameworkVersion,
                        baseResponse.language,
                        baseResponse.state,
                        baseResponse.activeSessionCount,
                        baseResponse.contextId,
                        baseResponse.isSessionSpecific,
                        baseResponse.defaultImports,
                        baseResponse.capabilities,
                        baseResponse.tips,
                        baseResponse.examples,
                        sessionMetadata = new
                        {
                            metadata.ContextId,
                            metadata.CreatedAt,
                            metadata.LastAccessedAt,
                            metadata.ExecutionCount,
                            metadata.IsInitialized,
                        },
                    }
                    : baseResponse
            )
            : baseResponse;
    }
}
