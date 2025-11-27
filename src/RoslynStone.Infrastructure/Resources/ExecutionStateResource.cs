using System.ComponentModel;
using System.Runtime.InteropServices;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.Resources;

/// <summary>
/// MCP resource for file-based C# execution environment information
/// </summary>
[McpServerResourceType]
public class ExecutionStateResource
{
    /// <summary>
    /// Get current file-based execution environment information as a resource
    /// </summary>
    /// <param name="contextManager">The execution context manager</param>
    /// <param name="requestContext">The request context containing the URI and optional params.</param>
    /// <returns>Information about current execution state</returns>
    [McpServerResource(
        UriTemplate = "repl://state",
        Name = "Execution Environment Information",
        MimeType = "application/json"
    )]
    [Description(
        "Access information about the file-based C# execution environment, including framework version, capabilities, and examples. NOTE: All executions are isolated and single-shot - no persistent state or sessions."
    )]
    public static ResourceContents GetReplState(
        IExecutionContextManager contextManager,
        [Description(
            "RequestContext containing the resource URI. Use 'repl://state' for execution environment information."
        )]
            RequestContext<ReadResourceRequestParams> requestContext
    )
    {
        var uri = requestContext.Params?.Uri ?? "repl://state";
        return GetReplState_Internal(contextManager, uri);
    }

    /// <summary>
    /// Get session-specific state information as a resource
    /// </summary>
    /// <param name="contextManager">The execution context manager</param>
    /// <param name="contextId">The ID of the session to query (path variable).</param>
    /// <param name="requestContext">The request context containing the URI and optional params.</param>
    /// <returns>Information about specific session state</returns>
    [McpServerResource(
        UriTemplate = "repl://sessions/{contextId}/state",
        Name = "Context Metadata Information",
        MimeType = "application/json"
    )]
    [Description(
        "Access metadata for a specific context ID used for execution tracking. Returns metadata including creation time, last accessed time, and execution count. NOTE: Context IDs track execution history only - they do NOT provide state persistence. All executions are isolated and independent. URI format: repl://sessions/{contextId}/state"
    )]
    public static ResourceContents GetReplSessionState(
        IExecutionContextManager contextManager,
        RequestContext<ReadResourceRequestParams> requestContext,
        [Description(
            "The session context ID extracted from the URI. Use 'repl://sessions/{contextId}/state' to target a specific session."
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
        IExecutionContextManager contextManager,
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
            "All executions are isolated and single-shot - no state persistence",
            "Each execution is independent - variables do NOT carry over",
            "Use 'using' directives to import additional namespaces",
            "Console.WriteLine output is captured separately from return values",
            "Top-level await is fully supported for async operations",
            "Use nugetPackages parameter to test packages inline",
            "Use #:package directive in final .cs files for self-contained utilities",
            "Use ValidateCsharp to check syntax before execution",
            "Use doc:// resources to learn about .NET APIs",
        };

        var capabilities = new ReplCapabilities
        {
            AsyncAwait = true,
            Linq = true,
            TopLevelStatements = true,
            ConsoleOutput = true,
            NugetPackages = true,
            Statefulness = false, // All executions are isolated and independent
        };

        var examples = new ReplExamples
        {
            SimpleExpression = "return 2 + 2;",
            VariableDeclaration = "var name = \"Alice\"; return name;",
            AsyncOperation = "await Task.Delay(100); return \"Done\";",
            LinqQuery = "return new[] { 1, 2, 3 }.Select(x => x * 2).ToArray();",
            ConsoleOutput = "Console.WriteLine(\"Debug\"); return \"Result\";",
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

        return new ExecutionStateResponse
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
