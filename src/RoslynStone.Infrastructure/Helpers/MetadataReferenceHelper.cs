using Microsoft.CodeAnalysis;
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
}
