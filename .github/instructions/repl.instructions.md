---
scope:
  languages:
    - csharp
  patterns:
    - "**/*repl*"
    - "**/*REPL*"
    - "**/*eval*"
    - "**/*interactive*"
---

# C# REPL Instructions

## Dogfooding: Test Your REPL Changes

When working on REPL functionality, **ALWAYS** use the MCP tools to test your changes:
- Use `EvaluateCsharp` to test the code patterns you're implementing
- Use `ValidateCsharp` to verify syntax of example code
- Use `GetReplInfo` to understand current REPL capabilities
- This ensures the REPL works correctly and validates the very functionality you're building

## REPL Implementation

When working on REPL (Read-Eval-Print Loop) functionality:

### Core Requirements
1. **Read**: Parse user input safely and correctly
2. **Eval**: Compile and execute C# code using Roslyn
3. **Print**: Return results in a structured, LLM-friendly format
4. **Loop**: Maintain state between evaluations

### Code Patterns

#### Script Execution
```csharp
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public async Task<object> EvaluateAsync(string code, ScriptState scriptState = null)
{
    try
    {
        var options = ScriptOptions.Default
            .AddReferences(GetAssemblyReferences())
            .AddImports(GetDefaultImports());
            
        if (scriptState == null)
        {
            scriptState = await CSharpScript.RunAsync(code, options);
        }
        else
        {
            scriptState = await scriptState.ContinueWithAsync(code, options);
        }
        
        return scriptState.ReturnValue;
    }
    catch (CompilationErrorException ex)
    {
        return CreateCompilationError(ex);
    }
}
```

#### Error Handling
Provide actionable error messages:
```csharp
private ErrorResponse CreateCompilationError(CompilationErrorException ex)
{
    return new ErrorResponse
    {
        Type = "CompilationError",
        Message = "Failed to compile the code",
        Diagnostics = ex.Diagnostics.Select(d => new Diagnostic
        {
            Severity = d.Severity.ToString(),
            Message = d.GetMessage(),
            Location = d.Location.GetLineSpan().ToString(),
            HelpLink = d.Descriptor.HelpLinkUri,
            Suggestion = GetSuggestion(d)
        }).ToList()
    };
}
```

### State Management

Maintain execution context across evaluations:
- Store variable definitions
- Preserve namespace imports
- Track assembly references
- Manage execution history

### NuGet Integration

Support dynamic package loading:
```csharp
public async Task<bool> AddPackageAsync(string packageId, string version = null)
{
    // Resolve package dependencies
    var packages = await ResolvePackageAsync(packageId, version);
    
    // Add assemblies to script options
    foreach (var package in packages)
    {
        AddAssemblyReference(package.AssemblyPath);
    }
    
    return true;
}
```

### Intellisense Support

Provide code completion and documentation:
- Use Roslyn's completion service
- Return symbol information
- Include XML documentation
- Support signature help

### LLM-Friendly Output

Structure responses for LLM consumption:
```csharp
public class EvaluationResult
{
    public bool Success { get; set; }
    public object Value { get; set; }
    public string Type { get; set; }
    public string FormattedValue { get; set; }
    public List<string> Warnings { get; set; }
    public ExecutionMetrics Metrics { get; set; }
}
```

## Security Considerations

### Code Sandboxing
- Restrict access to file system operations
- Limit network access
- Prevent infinite loops (timeout)
- Restrict memory usage
- Disable unsafe code by default

### Input Validation
- Validate code syntax before execution
- Check for prohibited patterns
- Sanitize user inputs
- Prevent code injection

## Performance Optimization

### Compilation Caching
- Cache compiled scripts when possible
- Reuse script state between evaluations
- Pre-load common assemblies
- Use incremental compilation

### Resource Management
- Implement execution timeouts
- Limit memory allocation
- Clean up resources properly
- Monitor execution metrics

## Testing

### Unit Tests
- Test valid C# code execution
- Test error handling for invalid code
- Test state persistence
- Test package loading
- Test security restrictions

### Integration Tests
- Test with complex C# scripts
- Test package dependencies
- Test long-running evaluations
- Test memory limits

## Documentation

Document REPL features:
- Supported C# language features
- Available commands (e.g., #load, #r)
- State management behavior
- Limitations and restrictions
- Usage examples

## Best Practices

1. **Isolation**: Isolate REPL sessions from each other
2. **Diagnostics**: Provide detailed diagnostic information
3. **Help**: Include built-in help system
4. **History**: Maintain command history
5. **Extensibility**: Support custom commands and extensions
6. **Formatting**: Pretty-print complex objects
7. **Async Support**: Handle async/await in user code
