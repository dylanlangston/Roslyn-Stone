namespace RoslynStone.Core.Models;

/// <summary>
/// Result of executing C# code in the REPL
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the execution was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the return value from the executed code
    /// </summary>
    public object? ReturnValue { get; init; }

    /// <summary>
    /// Gets or sets the console output captured during execution
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets or sets the compilation errors that occurred
    /// </summary>
    public IReadOnlyList<CompilationError> Errors { get; init; } = Array.Empty<CompilationError>();

    /// <summary>
    /// Gets or sets the compilation warnings that occurred
    /// </summary>
    public IReadOnlyList<CompilationError> Warnings { get; init; } =
        Array.Empty<CompilationError>();

    /// <summary>
    /// Gets or sets the total execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }
}

/// <summary>
/// Represents a compilation error or warning
/// </summary>
public class CompilationError
{
    /// <summary>
    /// Gets or sets the diagnostic code (e.g., CS0103)
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the error or warning message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level (Error, Warning, Info)
    /// </summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number where the issue occurred
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number where the issue occurred
    /// </summary>
    public int Column { get; init; }
}
