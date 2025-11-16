# Dynamic Compilation Best Practices

This document explains the best practices for dynamically compiling and executing C# code at runtime, based on Laurent Kempé's article "Dynamically compile and run code using .NET Core 3.0" ([article link](https://laurentkempe.com/2019/02/18/dynamically-compile-and-run-code-using-dotNET-Core-3.0/)).

## Overview

Dynamic compilation enables scenarios such as:
- Plugin architectures
- REPL (Read-Eval-Print Loop) implementations  
- Code evaluation services
- Hot-reloading of code without restarting the application
- Runtime code generation and execution

## Key Concepts

### 1. AssemblyLoadContext (Critical for .NET Core 3.0+)

**What it is**: A mechanism introduced in .NET Core that provides control over assembly loading and enables assembly unloading.

**Why it matters**:
- **Memory Management**: Without proper unloading, dynamically loaded assemblies stay in memory forever
- **Isolation**: Each context provides isolation between different versions of assemblies
- **Hot Reload**: Enables recompilation and reloading of code at runtime
- **Resource Cleanup**: Properly releases memory when assemblies are no longer needed

**Implementation**:
```csharp
public class UnloadableAssemblyLoadContext : AssemblyLoadContext
{
    public UnloadableAssemblyLoadContext() 
        : base(isCollectible: true) // CRITICAL: isCollectible must be true
    { }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Return null to use default loading behavior
        // This delegates to the default context for framework assemblies
        return null;
    }
}
```

**Key Points**:
- **`isCollectible: true`**: This is the critical parameter that enables assembly unloading
- Must be used for any dynamically loaded assemblies that should be unloadable
- Assemblies loaded in collectible contexts can be garbage collected after `Unload()` is called

### 2. WeakReference for Tracking Unloading

**Purpose**: Verify that assemblies are actually unloaded and garbage collected.

**Implementation**:
```csharp
var context = new UnloadableAssemblyLoadContext();
WeakReference contextWeakRef = new(context, trackResurrection: true);

try
{
    // Load and execute assembly
    var assembly = context.LoadFromStream(assemblyStream);
    // ... execute code ...
}
finally
{
    // Unload the context
    context.Unload();
    
    // Verify unloading by forcing garbage collection
    for (int i = 0; i < 10 && contextWeakRef.IsAlive; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    
    // If contextWeakRef.IsAlive is still true, something is holding a reference
}
```

**Key Points**:
- **`trackResurrection: true`**: Tracks the object even if it has a finalizer
- After `Unload()`, the weak reference should become dead after garbage collection
- If the weak reference stays alive, it indicates a memory leak (something is holding a reference)

### 3. Roslyn Compilation API

**Two Approaches**:

#### A. Roslyn Scripting API (Simpler, for REPL)
```csharp
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

var options = ScriptOptions.Default
    .WithReferences(typeof(object).Assembly)
    .WithImports("System", "System.Linq");

var result = await CSharpScript.RunAsync("1 + 1", options);
```

**Pros**:
- Very simple API
- Built-in state management between executions
- Good for REPL scenarios

**Cons**:
- Less control over compilation
- Cannot easily unload assemblies
- Not suitable when assembly isolation is needed

#### B. CSharpCompilation API (More Control)
```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var syntaxTree = CSharpSyntaxTree.ParseText(code);

var references = new[]
{
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
};

var compilation = CSharpCompilation.Create(
    "DynamicAssembly",
    syntaxTrees: new[] { syntaxTree },
    references: references,
    options: new CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release
    )
);

using var ms = new MemoryStream();
var emitResult = compilation.Emit(ms);

if (emitResult.Success)
{
    ms.Seek(0, SeekOrigin.Begin);
    var assembly = context.LoadFromStream(ms);
}
```

**Pros**:
- Full control over compilation process
- Can emit to memory streams for loading in custom contexts
- Better error diagnostics
- Suitable for production scenarios

**Cons**:
- More verbose
- Requires manual reference management

### 4. Reference Management

**Critical**: All assemblies and types used in the dynamic code must have their metadata references added to the compilation.

**Common References**:
```csharp
var references = new List<MetadataReference>
{
    // Core runtime
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
    
    // LINQ
    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    
    // System.Runtime (critical for .NET Core)
    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
    
    // Collections
    MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
    
    // For async/await
    MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
};
```

**Finding Additional References**:
```csharp
// For a specific type you need
var type = typeof(SomeType);
var reference = MetadataReference.CreateFromFile(type.Assembly.Location);

// For framework assemblies
var assembly = Assembly.Load("AssemblyName");
var reference = MetadataReference.CreateFromFile(assembly.Location);
```

### 5. Entry Point Discovery

When executing compiled assemblies, you need to find the entry point:

```csharp
private static MethodInfo? FindEntryPoint(Assembly assembly)
{
    // Traditional Main method
    var programType = assembly.GetTypes()
        .FirstOrDefault(t => t.Name == "Program");
    if (programType != null)
    {
        var mainMethod = programType.GetMethod("Main", 
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (mainMethod != null)
            return mainMethod;
    }
    
    // Top-level statements (C# 9+)
    var entryPoint = assembly.GetTypes()
        .SelectMany(t => t.GetMethods(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        .FirstOrDefault(m => m.Name == "<Main>$");
    
    return entryPoint;
}
```

**Execution**:
```csharp
var entryPoint = FindEntryPoint(assembly);
var parameters = entryPoint.GetParameters().Length == 0 
    ? null 
    : new object[] { Array.Empty<string>() };

var result = entryPoint.Invoke(null, parameters);

// Handle async returns
if (result is Task task)
{
    await task;
}
```

### 6. Console Output Capture

**For REPL scenarios**, capture Console output:

```csharp
var outputBuilder = new StringBuilder();
var originalOut = Console.Out;

try
{
    using var outputWriter = new StringWriter(outputBuilder);
    Console.SetOut(outputWriter);
    
    // Execute code
    
    await outputWriter.FlushAsync();
    var output = outputBuilder.ToString();
}
finally
{
    Console.SetOut(originalOut);
}
```

## Implementation in RoslynStone

### Architecture Decision

We use **both approaches** strategically:

1. **RoslynScriptingService** (Scripting API)
   - Used for REPL functionality
   - State preservation between executions
   - Simple expression evaluation
   - Quick prototyping

2. **CompilationService + AssemblyExecutionService** (Compilation API)
   - Used for file execution
   - Proper assembly unloading
   - Memory isolation
   - Production-grade execution

### Services Created

#### CompilationService
```csharp
// Compiles C# code to in-memory assemblies
public class CompilationService
{
    public CompilationResult Compile(string code, string? assemblyName = null)
    {
        // Uses CSharpCompilation API
        // Returns MemoryStream with compiled assembly
    }
}
```

#### AssemblyExecutionService
```csharp
// Executes assemblies in unloadable contexts
public class AssemblyExecutionService
{
    public async Task<AssemblyExecutionResult> ExecuteFileAsync(
        string filePath, 
        CancellationToken cancellationToken = default)
    {
        // 1. Compile code
        // 2. Create UnloadableAssemblyLoadContext
        // 3. Load assembly from stream
        // 4. Find and invoke entry point
        // 5. Unload context
        // 6. Verify unloading with WeakReference
    }
}
```

#### UnloadableAssemblyLoadContext
```csharp
// Custom context for assembly isolation
public class UnloadableAssemblyLoadContext : AssemblyLoadContext
{
    public UnloadableAssemblyLoadContext() 
        : base(isCollectible: true) { }
}
```

## Best Practices Summary

### ✅ DO

1. **Use AssemblyLoadContext** for any dynamically loaded assemblies
2. **Set isCollectible: true** when creating the context
3. **Use WeakReference** to verify unloading
4. **Call Unload()** and force garbage collection
5. **Manage metadata references** carefully
6. **Capture and handle compilation errors** properly
7. **Find entry points** for both traditional and top-level statements
8. **Handle async return types** (Task, Task<T>)
9. **Capture console output** if needed
10. **Dispose MemoryStreams** after loading assemblies

### ❌ DON'T

1. **Don't load assemblies in the default context** if you need to unload them
2. **Don't forget to call Unload()** on the context
3. **Don't hold references** to objects from the unloaded context
4. **Don't use the Scripting API** when you need assembly unloading
5. **Don't forget required assembly references** (System.Runtime is critical)
6. **Don't emit to disk** unless necessary (use MemoryStream)
7. **Don't forget to reset Console.Out** after capturing output
8. **Don't ignore compilation diagnostics**
9. **Don't assume synchronous execution** (handle Task returns)
10. **Don't forget to flush output writers** before reading captured output

## Memory Management Pattern

```csharp
// Correct pattern for dynamic compilation and execution
var context = new UnloadableAssemblyLoadContext();
WeakReference weakRef = new(context, trackResurrection: true);

try
{
    // 1. Compile
    var compilation = /* ... */;
    using var ms = new MemoryStream();
    var result = compilation.Emit(ms);
    
    // 2. Load
    ms.Seek(0, SeekOrigin.Begin);
    var assembly = context.LoadFromStream(ms);
    
    // 3. Execute
    var entryPoint = FindEntryPoint(assembly);
    entryPoint.Invoke(null, parameters);
}
finally
{
    // 4. Unload
    context.Unload();
    
    // 5. Verify unloading
    for (int i = 0; i < 10 && weakRef.IsAlive; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    
    if (weakRef.IsAlive)
    {
        // Memory leak detected - something is still holding a reference
        Console.WriteLine("Warning: Assembly context was not unloaded");
    }
}
```

## Security Considerations

1. **Code Execution Risk**: Dynamic compilation executes arbitrary code
   - Run in sandboxed environments
   - Implement code review/validation
   - Use least-privilege execution

2. **Resource Limits**: 
   - Set execution timeouts
   - Monitor memory usage
   - Limit CPU usage

3. **Assembly References**:
   - Only add necessary references
   - Avoid loading privileged assemblies
   - Validate assembly sources

## Performance Considerations

1. **First Compilation**: ~500-1000ms (includes JIT)
2. **Subsequent Compilations**: ~200-300ms
3. **Unloading**: ~50-100ms (with forced GC)
4. **Memory**: Each loaded assembly context adds ~1-5MB overhead

**Optimization Tips**:
- Cache compilation results when possible
- Reuse AssemblyLoadContext instances for similar operations
- Batch multiple compilations
- Use OptimizationLevel.Release for production

## Testing

Essential tests to include:

```csharp
[Fact]
public async Task Assembly_CanBeUnloaded()
{
    WeakReference weakRef = null;
    
    {
        var context = new UnloadableAssemblyLoadContext();
        weakRef = new WeakReference(context, trackResurrection: true);
        
        // Load and execute assembly
        
        context.Unload();
    }
    
    // Force GC
    for (int i = 0; i < 10; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    
    Assert.False(weakRef.IsAlive, "Assembly context was not unloaded");
}
```

## References

- [Laurent Kempé's Article](https://laurentkempe.com/2019/02/18/dynamically-compile-and-run-code-using-dotNET-Core-3.0/)
- [GitHub - DynamicRun Project](https://github.com/laurentkempe/DynamicRun)
- [Microsoft Docs - AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- [Microsoft Docs - Roslyn APIs](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [Stack Overflow - Dynamic Compilation in .NET Core](https://stackoverflow.com/questions/71474900/dynamic-compilation-in-net-core-6)

## Conclusion

The key insight from Laurent Kempé's approach is that **AssemblyLoadContext with `isCollectible: true` is essential** for proper memory management in dynamic compilation scenarios. Without it, every dynamically loaded assembly stays in memory forever, leading to memory leaks.

Combined with proper use of:
- Roslyn's CSharpCompilation API
- WeakReference for verification
- Correct reference management
- Proper entry point discovery

This approach enables production-grade dynamic code execution with full control over memory lifecycle.
