using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using RoslynStone.Infrastructure.Helpers;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for compiling C# code to assemblies using Roslyn
/// Based on best practices from Laurent Kempé's dynamic compilation approach
/// </summary>
public class CompilationService
{
    private readonly ScriptOptions _scriptOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationService"/> class
    /// </summary>
    public CompilationService()
    {
        _scriptOptions = MetadataReferenceHelper.GetDefaultScriptOptions();
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

        // Enable file-based program features to support #:package, #:sdk, #:property directives
        var parseOptions = MetadataReferenceHelper.GetFileBasedProgramParseOptions();

        var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);

        // Get metadata references from script options (already configured in constructor)
        var references = _scriptOptions
            .MetadataReferences.OfType<PortableExecutableReference>()
            .ToList();

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication, // Changed from DynamicallyLinkedLibrary to support top-level statements
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
                    diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error
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
    /// <summary>
    /// Gets or sets a value indicating whether the compilation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the name of the compiled assembly
    /// </summary>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// Gets or sets the stream containing the compiled assembly
    /// </summary>
    public MemoryStream? AssemblyStream { get; set; }

    /// <summary>
    /// Gets or sets the stream containing the debug symbols
    /// </summary>
    public MemoryStream? SymbolsStream { get; set; }

    /// <summary>
    /// Gets or sets the compilation diagnostics
    /// </summary>
    public List<Diagnostic>? Diagnostics { get; set; }

    /// <summary>
    /// Gets or sets the error messages from compilation
    /// </summary>
    public List<string>? ErrorMessages { get; set; }
}

/// <summary>
/// Custom AssemblyLoadContext that can be unloaded
/// Based on Laurent Kempé's approach for proper memory management
/// </summary>
public class UnloadableAssemblyLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnloadableAssemblyLoadContext"/> class
    /// </summary>
    public UnloadableAssemblyLoadContext()
        : base(isCollectible: true) { }

    /// <summary>
    /// Resolves an assembly by name
    /// </summary>
    /// <param name="assemblyName">The assembly name to resolve</param>
    /// <returns>The loaded assembly or null to use default loading behavior</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Return null to use default loading behavior
        return null;
    }
}
