using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Functional;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for executing C# code using Roslyn scripting engine
/// Thread-safe singleton service for REPL state management
///
/// Locking Strategy:
/// - Uses a static semaphore to serialize all code execution across instances
/// - This prevents Console.SetOut() interference between parallel tests
/// - Individual methods that don't use Console.SetOut() use instance lock only
/// </summary>
public class RoslynScriptingService
{
    // Static semaphore to serialize code execution across all instances
    // This prevents Console.SetOut() interference in parallel tests
    private static readonly SemaphoreSlim _executionLock = new(1, 1);

    private ScriptState? _scriptState;
    private ScriptOptions _scriptOptions;
    private readonly StringWriter _outputWriter;
    private readonly SemaphoreSlim _stateLock = new(1, 1); // Protects instance state

    /// <summary>
    /// Gets the script options used for compilation
    /// </summary>
    public ScriptOptions ScriptOptions => _scriptOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynScriptingService"/> class
    /// </summary>
    public RoslynScriptingService()
    {
        _outputWriter = new StringWriter();

        // Configure script options with common assemblies
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
    /// Execute C# code and return the result
    /// </summary>
    /// <param name="code">The C# code to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Execution result with return value, output, errors, and timing information</returns>
    public async Task<ExecutionResult> ExecuteAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<CompilationError>();
        var warnings = new List<CompilationError>();

        // Use static execution lock to prevent Console.SetOut() interference
        // This serializes all code execution across instances for test safety
        await _executionLock.WaitAsync(cancellationToken);
        try
        {
            // Capture console output
            var originalOut = Console.Out;
            Console.SetOut(_outputWriter);

            try
            {
                // Continue from previous state or start new
                _scriptState =
                    _scriptState == null
                        ? await CSharpScript.RunAsync(
                            code,
                            _scriptOptions,
                            cancellationToken: cancellationToken
                        )
                        : await _scriptState.ContinueWithAsync(
                            code,
                            cancellationToken: cancellationToken
                        );

                stopwatch.Stop();

                // Ensure all output is flushed
                await Console.Out.FlushAsync();

                // Get the current output
                var output = _outputWriter.ToString();

                // Clear the buffer for next execution
                var sb = _outputWriter.GetStringBuilder();
                sb.Clear();

                return new ExecutionResult
                {
                    Success = true,
                    ReturnValue = _scriptState.ReturnValue,
                    Output = output,
                    Errors = errors,
                    Warnings = warnings,
                    ExecutionTime = stopwatch.Elapsed,
                    ScriptState = _scriptState,
                };
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        catch (CompilationErrorException ex)
        {
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                Errors = ex.Diagnostics.ToCompilationErrors(),
                ExecutionTime = stopwatch.Elapsed,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "RUNTIME_ERROR",
                        Message = ex.Message,
                        Severity = "Error",
                    },
                ],
                ExecutionTime = stopwatch.Elapsed,
            };
        }
        finally
        {
            _executionLock.Release();
        }
    }

    /// <summary>
    /// Add a NuGet package reference to the script options
    /// </summary>
    /// <param name="packageName">Name of the NuGet package</param>
    /// <param name="version">Optional package version</param>
    /// <param name="assemblyPaths">List of assembly file paths to add</param>
    public async Task AddPackageReferenceAsync(
        string packageName,
        string? version = null,
        List<string>? assemblyPaths = null
    )
    {
        await _stateLock.WaitAsync();
        try
        {
            if (assemblyPaths != null && assemblyPaths.Count > 0)
            {
                // Add each assembly to script options
                foreach (var assemblyPath in assemblyPaths.Where(File.Exists))
                {
                    _scriptOptions = _scriptOptions.AddReferences(Assembly.LoadFrom(assemblyPath));
                }

                // Reset script state to apply new references
                _scriptState = null;
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Reset the script state
    /// </summary>
    public void Reset()
    {
        _stateLock.Wait();
        try
        {
            _scriptState = null;
            _outputWriter.GetStringBuilder().Clear();
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Execute C# code with an existing script state (for context-aware execution)
    /// </summary>
    /// <param name="code">The C# code to execute</param>
    /// <param name="existingState">The existing script state to continue from (can be null for new state)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Execution result with return value, output, errors, timing information, and updated script state</returns>
    public async Task<ExecutionResult> ExecuteWithStateAsync(
        string code,
        ScriptState? existingState,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<CompilationError>();
        var warnings = new List<CompilationError>();

        // Use static execution lock to prevent Console.SetOut() interference
        await _executionLock.WaitAsync(cancellationToken);
        try
        {
            // Capture console output
            var originalOut = Console.Out;
            using var tempWriter = new StringWriter();
            Console.SetOut(tempWriter);

            try
            {
                // Continue from provided state or start new
                ScriptState<object>? newState;
                if (existingState == null)
                {
                    newState = await CSharpScript.RunAsync(
                        code,
                        _scriptOptions,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    newState = await existingState.ContinueWithAsync(
                        code,
                        cancellationToken: cancellationToken
                    );
                }

                stopwatch.Stop();

                // Ensure all output is flushed
                await Console.Out.FlushAsync();

                // Get the current output
                var output = tempWriter.ToString();

                return new ExecutionResult
                {
                    Success = true,
                    ReturnValue = newState.ReturnValue,
                    Output = output,
                    Errors = errors,
                    Warnings = warnings,
                    ExecutionTime = stopwatch.Elapsed,
                    ScriptState = newState, // Return the new state
                };
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        catch (CompilationErrorException ex)
        {
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                Errors = ex.Diagnostics.ToCompilationErrors(),
                ExecutionTime = stopwatch.Elapsed,
                ScriptState = existingState, // Keep existing state on error
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "RUNTIME_ERROR",
                        Message = ex.Message,
                        Severity = "Error",
                    },
                ],
                ExecutionTime = stopwatch.Elapsed,
                ScriptState = existingState, // Keep existing state on error
            };
        }
        finally
        {
            _executionLock.Release();
        }
    }
}
