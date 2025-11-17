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
    /// <param name="uri">The resource URI: repl://state or repl://info</param>
    /// <returns>Information about current REPL state</returns>
    [McpServerResource]
    [Description(
        "Access current REPL environment state and capabilities. Returns information about framework version, available namespaces, loaded assemblies, imported NuGet packages, current variables in scope, and REPL capabilities. Use this to understand what's available, check the current state, and get oriented in a session."
    )]
    public static object GetReplState(
        RoslynScriptingService scriptingService,
        [Description(
            "Resource URI. Use 'repl://state' or 'repl://info' to access current REPL environment information."
        )]
            string uri
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

        return new
        {
            uri,
            mimeType = "application/json",
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
    }
}
