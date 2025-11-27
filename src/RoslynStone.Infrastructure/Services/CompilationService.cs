using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
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

        // Parse with latest language features to support modern C# syntax (including proper string interpolation)
        var parseOptions = CSharpParseOptions
            .Default.WithKind(SourceCodeKind.Regular)
            .WithLanguageVersion(LanguageVersion.Preview);

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
/// Validates assembly loading against a blocklist when configured
/// </summary>
public class UnloadableAssemblyLoadContext : AssemblyLoadContext
{
    private readonly IReadOnlyList<string>? _blockedAssemblies;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnloadableAssemblyLoadContext"/> class
    /// </summary>
    /// <param name="blockedAssemblies">Optional list of blocked assembly names (null = allow all)</param>
    /// <param name="logger">Optional logger for tracking blocked assemblies</param>
    public UnloadableAssemblyLoadContext(
        IReadOnlyList<string>? blockedAssemblies = null,
        ILogger? logger = null
    )
        : base(isCollectible: true)
    {
        _blockedAssemblies = blockedAssemblies;
        _logger = logger;
    }

    /// <summary>
    /// Resolves an assembly by name
    /// Blocks assemblies in the blocklist if configured
    /// </summary>
    /// <param name="assemblyName">The assembly name to resolve</param>
    /// <returns>The loaded assembly or null to use default loading behavior</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name))
            return null;

        // If no blocklist configured, use default behavior
        if (_blockedAssemblies == null || _blockedAssemblies.Count == 0)
            return null;

        // Check if assembly is in blocklist
        var isBlocked = _blockedAssemblies.Any(blocked =>
            name.Equals(blocked, StringComparison.OrdinalIgnoreCase)
            || name.StartsWith($"{blocked}.", StringComparison.OrdinalIgnoreCase)
        );

        if (isBlocked)
        {
            _logger?.LogWarning(
                "Blocked assembly load attempt: {AssemblyName} is in the blocklist",
                name
            );
            throw new FileLoadException(
                $"Assembly '{name}' is blocked for security reasons. "
                    + $"This assembly provides dangerous APIs that could be used maliciously."
            );
        }

        // Return null to use default loading behavior for allowed assemblies
        return null;
    }
}
