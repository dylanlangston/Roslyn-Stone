using Microsoft.CodeAnalysis.Scripting;

namespace RoslynStone.Core.Models;

/// <summary>
/// Result of executing C# code in the REPL
/// </summary>
public record ExecutionResult
{
    /// <summary>
    /// Gets a value indicating whether the execution was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the return value from the executed code
    /// </summary>
    public object? ReturnValue { get; init; }

    /// <summary>
    /// Gets the console output captured during execution
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets the compilation errors that occurred
    /// </summary>
    public IReadOnlyList<CompilationError> Errors { get; init; } = Array.Empty<CompilationError>();

    /// <summary>
    /// Gets the compilation warnings that occurred
    /// </summary>
    public IReadOnlyList<CompilationError> Warnings { get; init; } =
        Array.Empty<CompilationError>();

    /// <summary>
    /// Gets the total execution time
    /// </summary>
    public required TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Gets the resulting script state (for context-aware execution)
    /// </summary>
    public ScriptState? ScriptState { get; init; }
}

/// <summary>
/// Represents a compilation error or warning
/// </summary>
public record CompilationError
{
    /// <summary>
    /// Gets the diagnostic code (e.g., CS0103)
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error or warning message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the severity level (Error, Warning, Info)
    /// </summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>
    /// Gets the line number where the issue occurred
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Gets the column number where the issue occurred
    /// </summary>
    public int Column { get; init; }
}
