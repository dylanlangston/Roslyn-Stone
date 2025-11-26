using System.ComponentModel;
using System.Runtime.InteropServices;
using ModelContextProtocol.Protocol;
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
    /// <param name="requestContext">The request context containing the URI and optional params.</param>
    /// <returns>Information about current REPL state</returns>
    [McpServerResource(
        UriTemplate = "repl://state",
        Name = "REPL State Information",
        MimeType = "application/json"
    )]
    [Description(
        "Access the current REPL environment state, including active sessions, default imports, capabilities, tips, and examples."
    )]
    public static ResourceContents GetReplState(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        [Description(
            "RequestContext containing the resource URI. Use 'repl://state' or 'repl://info' for general info, or 'repl://sessions/{contextId}/state' for session-specific state."
        )]
            RequestContext<ReadResourceRequestParams> requestContext
    )
    {
        var uri = requestContext.Params?.Uri ?? "repl://state";
        return GetReplState_Internal(contextManager, uri);
    }

    /// <summary>
    /// Get REPL session-specific state information as a resource
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="contextManager">The REPL context manager</param>
    /// <param name="contextId">The ID of the REPL session to query (path variable).</param>
    /// <param name="requestContext">The request context containing the URI and optional params.</param>
    /// <returns>Information about specific REPL session state</returns>
    [McpServerResource(
        UriTemplate = "repl://sessions/{contextId}/state",
        Name = "REPL Session State Information",
        MimeType = "application/json"
    )]
    [Description(
        "Access detailed state information for a specific REPL session identified by its context ID. Returns session metadata including creation time, last accessed time, execution count, and initialization status. Use this to monitor and manage individual REPL sessions, track their activity, and understand their current state. URI format: repl://sessions/{contextId}/state"
    )]
    public static ResourceContents GetReplSessionState(
        RoslynScriptingService scriptingService,
        IReplContextManager contextManager,
        RequestContext<ReadResourceRequestParams> requestContext,
        [Description(
            "The REPL session context ID extracted from the URI. Use 'repl://sessions/{contextId}/state' to target a specific session."
        )]
            string contextId
    )
    {
        // Construct URI from the provided contextId if available, otherwise fall back to requestContext
        var uri = requestContext.Params?.Uri ?? $"repl://sessions/{contextId}/state";
        return GetReplState_Internal(contextManager, uri);
    }

    /// <summary>
    /// Internal method to get REPL state information
    /// </summary>
    /// <param name="contextManager"></param>
    /// <param name="uri"></param>
    /// <returns></returns>
    static ResourceContents GetReplState_Internal(
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
