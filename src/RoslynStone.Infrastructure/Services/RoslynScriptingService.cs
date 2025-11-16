using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RoslynStone.Core.Models;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for executing C# code using Roslyn scripting engine
/// Thread-safe singleton service for REPL state management
/// </summary>
public class RoslynScriptingService
{
    private ScriptState? _scriptState;
    private readonly ScriptOptions _scriptOptions;
    private readonly StringWriter _outputWriter;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Thread-safe async execution

    public ScriptOptions ScriptOptions => _scriptOptions;

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

        // Use SemaphoreSlim for thread-safe async execution
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Capture console output
            var originalOut = Console.Out;
            Console.SetOut(_outputWriter);

            try
            {
                // Continue from previous state or start new
                _scriptState = _scriptState == null
                    ? await CSharpScript.RunAsync(code, _scriptOptions, cancellationToken: cancellationToken)
                    : await _scriptState.ContinueWithAsync(code, cancellationToken: cancellationToken);

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

            foreach (var diagnostic in ex.Diagnostics)
            {
                errors.Add(
                    new CompilationError
                    {
                        Code = diagnostic.Id,
                        Message = diagnostic.GetMessage(),
                        Severity = diagnostic.Severity.ToString(),
                        Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                        Column =
                            diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                    }
                );
            }

            return new ExecutionResult
            {
                Success = false,
                Errors = errors,
                ExecutionTime = stopwatch.Elapsed,
            };
        }
        catch (Exception ex)
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
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Add a NuGet package reference to the script options
    /// </summary>
    /// <param name="packageName">Name of the NuGet package</param>
    /// <param name="version">Optional package version</param>
    public void AddPackageReference(string packageName, string? version = null)
    {
        // Note: For full NuGet support, we would need to integrate with NuGet.Protocol
        // For now, we can add assembly references if the package is already restored
        // This is a simplified version
    }

    /// <summary>
    /// Reset the script state
    /// </summary>
    public void Reset()
    {
        _semaphore.Wait();
        try
        {
            _scriptState = null;
            _outputWriter.GetStringBuilder().Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
