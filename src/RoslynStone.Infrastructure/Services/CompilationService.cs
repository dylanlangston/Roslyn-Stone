using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for compiling C# code to assemblies using Roslyn
/// Based on best practices from Laurent Kempé's dynamic compilation approach
/// </summary>
public class CompilationService
{
    private readonly ScriptOptions _scriptOptions;

    public CompilationService()
    {
        // Configure default references
        _scriptOptions = ScriptOptions
            .Default.WithReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(Console).Assembly,
                Assembly.Load("System.Runtime"),
                Assembly.Load("System.Collections")
            )
            .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks"
            );
    }

    /// <summary>
    /// Compile C# code to an in-memory assembly
    /// </summary>
    /// <param name="code">The C# source code to compile</param>
    /// <param name="assemblyName">Optional assembly name</param>
    /// <returns>Compilation result with the compiled assembly stream or errors</returns>
    public CompilationResult Compile(string code, string? assemblyName = null)
    {
        assemblyName ??= $"DynamicAssembly_{Guid.NewGuid():N}";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        // Get metadata references from existing assemblies
        var references = _scriptOptions.MetadataReferences
            .OfType<PortableExecutableReference>()
            .ToList();

        // Add additional required references
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        );
        references.Add(
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        );

        // Add System.Runtime
        var runtimeAssembly = Assembly.Load("System.Runtime");
        references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,  // Changed from DynamicallyLinkedLibrary to support top-level statements
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false
            )
        );

        // Emit to memory stream
        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();

        var emitResult = compilation.Emit(peStream, pdbStream);

        if (!emitResult.Success)
        {
            var failures = emitResult
                .Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError
                    || diagnostic.Severity == DiagnosticSeverity.Error
                )
                .ToList();

            return new CompilationResult
            {
                Success = false,
                Diagnostics = failures,
                ErrorMessages = failures
                    .Select(d => $"{d.Id}: {d.GetMessage()} at {d.Location.GetLineSpan()}")
                    .ToList(),
            };
        }

        // Reset stream positions
        peStream.Seek(0, SeekOrigin.Begin);
        pdbStream.Seek(0, SeekOrigin.Begin);

        // Copy to new streams that won't be disposed
        var assemblyStream = new MemoryStream();
        var symbolsStream = new MemoryStream();
        peStream.CopyTo(assemblyStream);
        pdbStream.CopyTo(symbolsStream);
        assemblyStream.Seek(0, SeekOrigin.Begin);
        symbolsStream.Seek(0, SeekOrigin.Begin);

        return new CompilationResult
        {
            Success = true,
            AssemblyName = assemblyName,
            AssemblyStream = assemblyStream,
            SymbolsStream = symbolsStream,
        };
    }
}

/// <summary>
/// Result of a compilation operation
/// </summary>
public class CompilationResult
{
    public bool Success { get; set; }
    public string? AssemblyName { get; set; }
    public MemoryStream? AssemblyStream { get; set; }
    public MemoryStream? SymbolsStream { get; set; }
    public List<Diagnostic>? Diagnostics { get; set; }
    public List<string>? ErrorMessages { get; set; }
}

/// <summary>
/// Custom AssemblyLoadContext that can be unloaded
/// Based on Laurent Kempé's approach for proper memory management
/// </summary>
public class UnloadableAssemblyLoadContext : AssemblyLoadContext
{
    public UnloadableAssemblyLoadContext()
        : base(isCollectible: true) { }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Return null to use default loading behavior
        return null;
    }
}
