using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynStone.Infrastructure.Helpers;

/// <summary>
/// Helper class for creating default metadata references for Roslyn compilation
/// </summary>
public static class MetadataReferenceHelper
{
    /// <summary>
    /// Gets the default metadata references required for C# compilation
    /// Includes System.Private.CoreLib, System.Linq, System.Console, System.Runtime, and System.Collections
    /// </summary>
    /// <returns>List of metadata references</returns>
    public static List<MetadataReference> GetDefaultReferences()
    {
        // Configure default references using only MetadataReference
        // This avoids any Assembly.Load() calls
        // We need both System.Private.CoreLib (actual types) and System.Runtime (facade/reference assembly)
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Private.CoreLib
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location), // System.Console
        };

        // Add System.Runtime facade assembly
        // In .NET Core/.NET 5+, many assemblies reference System.Runtime even though types are in System.Private.CoreLib
        var coreLibDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        ArgumentNullException.ThrowIfNull(coreLibDirectory);

        var runtimeAssemblyPath = Path.Combine(coreLibDirectory, "System.Runtime.dll");
        if (File.Exists(runtimeAssemblyPath))
        {
            refs.Add(MetadataReference.CreateFromFile(runtimeAssemblyPath));
        }

        // Add System.Collections
        var collectionsAssemblyPath = Path.Combine(coreLibDirectory, "System.Collections.dll");
        if (File.Exists(collectionsAssemblyPath))
        {
            refs.Add(MetadataReference.CreateFromFile(collectionsAssemblyPath));
        }

        return refs;
    }

    /// <summary>
    /// Gets default script options with common references and imports
    /// </summary>
    /// <returns>Configured ScriptOptions instance</returns>
    public static ScriptOptions GetDefaultScriptOptions()
    {
        return ScriptOptions
            .Default.AddReferences(GetDefaultReferences())
            .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks"
            );
    }

    /// <summary>
    /// Gets CSharpParseOptions with file-based program features enabled
    /// This allows #:package, #:sdk, #:property directives to be recognized
    /// </summary>
    /// <returns>Configured CSharpParseOptions instance</returns>
    public static CSharpParseOptions GetFileBasedProgramParseOptions()
    {
        return CSharpParseOptions
            .Default.WithLanguageVersion(LanguageVersion.Preview)
            .WithFeatures([new KeyValuePair<string, string>("FileBasedProgram", "")]);
    }

    /// <summary>
    /// Strips file-based program directives (#:package, #:sdk, #:property, #:project) from code
    /// These directives are build-time directives processed by the SDK, not the Roslyn scripting engine
    /// </summary>
    /// <param name="code">The source code that may contain file-based program directives</param>
    /// <returns>Code with file-based program directives removed</returns>
    public static string StripFileBasedProgramDirectives(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        // Split on all common line ending styles to handle cross-platform code
        var lines = code.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var filteredLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();
            // Skip lines that start with #: (file-based program directives)
            // Also skip shebang lines (#!)
            if (
                !trimmedLine.StartsWith("#:", StringComparison.Ordinal)
                && !trimmedLine.StartsWith("#!", StringComparison.Ordinal)
            )
            {
                filteredLines.Add(line);
            }
        }

        // Preserve the original line ending style by detecting it from the input
        var lineEnding =
            code.Contains("\r\n") ? "\r\n"
            : code.Contains("\r") ? "\r"
            : "\n";
        return string.Join(lineEnding, filteredLines);
    }
}
