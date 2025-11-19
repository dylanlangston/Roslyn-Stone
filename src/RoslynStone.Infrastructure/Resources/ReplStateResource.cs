using System.ComponentModel;
using System.Runtime.InteropServices;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Models;
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
        "Access current REPL environment state and capabilities. Returns information about framework version, available namespaces, loaded assemblies, imported NuGet packages, current variables in scope, and REPL capabilities. Use 'repl://state' or 'repl://info' for general info, or 'repl://sessions/{contextId}/state' for session-specific state. URI formats: repl://state, repl://info, repl://sessions/{contextId}/state"
    )]
    public static ReplStateResponse GetReplState(
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
            if (
                sessionIndex >= 0
                && sessionIndex + 1 < parts.Length
                && !string.IsNullOrWhiteSpace(parts[sessionIndex + 1])
            )
            {
                contextId = parts[sessionIndex + 1];
                // Validate that it's followed by 'state' as expected
                if (
                    sessionIndex + 2 >= parts.Length
                    || !parts[sessionIndex + 2].Equals("state", StringComparison.OrdinalIgnoreCase)
                )
                {
                    contextId = null; // Invalid format, reset
                }
            }
        }

        var imports = new List<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Threading.Tasks",
        };

        // Get session information from context manager
        var activeSessionCount = contextManager.GetActiveContexts().Count;

        var tips = new List<string>
        {
            "Variables and types persist between executions",
            "Use 'using' directives to import additional namespaces",
            "Console.WriteLine output is captured separately from return values",
            "Async/await is fully supported in the REPL",
            "Use LoadNuGetPackage to add external libraries",
            "Use ResetRepl to clear all state and start fresh",
            "Use ValidateCsharp to check syntax before execution",
            "Use documentation resources to learn about .NET APIs",
        };

        var capabilities = new ReplCapabilities
        {
            AsyncAwait = true,
            Linq = true,
            TopLevelStatements = true,
            ConsoleOutput = true,
            NugetPackages = true,
            Statefulness = true,
        };

        var examples = new ReplExamples
        {
            SimpleExpression = "2 + 2",
            VariableDeclaration = "var name = \"Alice\"; name",
            AsyncOperation = "await Task.Delay(100); \"Done\"",
            LinqQuery = "new[] { 1, 2, 3 }.Select(x => x * 2)",
            ConsoleOutput = "Console.WriteLine(\"Debug\"); return \"Result\"",
        };

        SessionMetadata? sessionMetadata = null;
        if (isSessionSpecific && !string.IsNullOrEmpty(contextId))
        {
            var metadata = contextManager.GetContextMetadata(contextId);
            if (metadata != null)
            {
                sessionMetadata = new SessionMetadata
                {
                    ContextId = metadata.ContextId,
                    CreatedAt = metadata.CreatedAt,
                    LastAccessedAt = metadata.LastAccessedAt,
                    ExecutionCount = metadata.ExecutionCount,
                    IsInitialized = metadata.IsInitialized,
                };
            }
        }

        return new ReplStateResponse
        {
            Uri = uri,
            MimeType = "application/json",
            FrameworkVersion = RuntimeInformation.FrameworkDescription,
            Language = "C# 14",
            State = "Ready",
            ActiveSessionCount = activeSessionCount,
            ContextId = contextId,
            IsSessionSpecific = isSessionSpecific,
            DefaultImports = imports,
            Capabilities = capabilities,
            Tips = tips,
            Examples = examples,
            SessionMetadata = sessionMetadata,
        };
    }
}
