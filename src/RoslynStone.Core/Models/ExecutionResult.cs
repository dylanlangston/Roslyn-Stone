namespace RoslynStone.Core.Models;

/// <summary>
/// Result of executing C# code in the REPL
/// </summary>
public class ExecutionResult
{
    public bool Success { get; set; }
    public object? ReturnValue { get; set; }
    public string? Output { get; set; }
    public IReadOnlyList<CompilationError> Errors { get; set; } = Array.Empty<CompilationError>();
    public IReadOnlyList<CompilationError> Warnings { get; set; } = Array.Empty<CompilationError>();
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Represents a compilation error or warning
/// </summary>
public class CompilationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}
