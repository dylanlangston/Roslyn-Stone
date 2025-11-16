using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RoslynStone.Core.Models;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for executing C# code using Roslyn scripting engine
/// </summary>
public class RoslynScriptingService
{
    private ScriptState? _scriptState;
    private readonly ScriptOptions _scriptOptions;
    private readonly StringWriter _outputWriter;

    public ScriptOptions ScriptOptions => _scriptOptions;

    public RoslynScriptingService()
    {
        _outputWriter = new StringWriter();
        
        // Configure script options with common assemblies
        _scriptOptions = ScriptOptions.Default
            .WithReferences(
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
    public async Task<ExecutionResult> ExecuteAsync(string code, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<CompilationError>();
        var warnings = new List<CompilationError>();

        try
        {
            // Capture console output
            var originalOut = Console.Out;
            Console.SetOut(_outputWriter);

            try
            {
                // Continue from previous state or start new
                if (_scriptState == null)
                {
                    _scriptState = await CSharpScript.RunAsync(code, _scriptOptions, cancellationToken: cancellationToken);
                }
                else
                {
                    _scriptState = await _scriptState.ContinueWithAsync(code, cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

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
                    ExecutionTime = stopwatch.Elapsed
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
                errors.Add(new CompilationError
                {
                    Code = diagnostic.Id,
                    Message = diagnostic.GetMessage(),
                    Severity = diagnostic.Severity.ToString(),
                    Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                    Column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1
                });
            }

            return new ExecutionResult
            {
                Success = false,
                Errors = errors,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            return new ExecutionResult
            {
                Success = false,
                Errors = new List<CompilationError>
                {
                    new CompilationError
                    {
                        Code = "RUNTIME_ERROR",
                        Message = ex.Message,
                        Severity = "Error"
                    }
                },
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Add a NuGet package reference to the script options
    /// </summary>
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
        _scriptState = null;
        _outputWriter.GetStringBuilder().Clear();
    }
}
