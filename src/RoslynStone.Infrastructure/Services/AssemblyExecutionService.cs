using System.Reflection;
using System.Text;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for executing compiled assemblies with proper isolation and cleanup
/// Based on Laurent Kempé's approach using AssemblyLoadContext
/// </summary>
public class AssemblyExecutionService
{
    private readonly CompilationService _compilationService;

    public AssemblyExecutionService(CompilationService compilationService)
    {
        _compilationService = compilationService;
    }

    /// <summary>
    /// Execute C# code from a file by compiling and loading it in an unloadable context
    /// </summary>
    /// <param name="filePath">Path to the C# source file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result with output and any errors</returns>
    public async Task<AssemblyExecutionResult> ExecuteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        if (!File.Exists(filePath))
        {
            return new AssemblyExecutionResult
            {
                Success = false,
                ErrorMessage = $"File not found: {filePath}",
            };
        }

        string code;
        try
        {
            code = await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            return new AssemblyExecutionResult
            {
                Success = false,
                ErrorMessage = $"Failed to read file: {ex.Message}",
            };
        }

        return await ExecuteCodeAsync(code, cancellationToken);
    }

    /// <summary>
    /// Execute C# code by compiling and loading it in an unloadable context
    /// </summary>
    /// <param name="code">C# source code to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result with output and any errors</returns>
    public async Task<AssemblyExecutionResult> ExecuteCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        // Compile the code
        var compilationResult = _compilationService.Compile(code);

        if (!compilationResult.Success)
        {
            return new AssemblyExecutionResult
            {
                Success = false,
                ErrorMessage = "Compilation failed",
                CompilationErrors = compilationResult.ErrorMessages,
            };
        }

        // Execute in an unloadable context
        return await ExecuteInUnloadableContextAsync(
            compilationResult,
            cancellationToken
        );
    }

    /// <summary>
    /// Execute an assembly in an unloadable AssemblyLoadContext
    /// </summary>
    private async Task<AssemblyExecutionResult> ExecuteInUnloadableContextAsync(
        CompilationResult compilationResult,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(compilationResult.AssemblyStream);

        var context = new UnloadableAssemblyLoadContext();
        WeakReference contextWeakRef = new(context, trackResurrection: true);

        try
        {
            // Load the assembly from the memory stream
            var assembly = context.LoadFromStream(compilationResult.AssemblyStream);

            // Find the entry point
            var entryPoint = FindEntryPoint(assembly);
            if (entryPoint == null)
            {
                return new AssemblyExecutionResult
                {
                    Success = false,
                    ErrorMessage =
                        "No entry point found. Ensure the code has a Main method or top-level statements.",
                };
            }

            // Capture console output
            var outputBuilder = new StringBuilder();
            var originalOut = Console.Out;
            var originalError = Console.Error;

            try
            {
                using var outputWriter = new StringWriter(outputBuilder);
                Console.SetOut(outputWriter);
                Console.SetError(outputWriter);

                // Execute the entry point
                var result = entryPoint.Invoke(
                    null,
                    entryPoint.GetParameters().Length == 0 ? null : new object[] { Array.Empty<string>() }
                );

                // Handle async Task return types
                if (result is Task task)
                {
                    await task.WaitAsync(cancellationToken);
                }

                // Flush output after async completion
                await Console.Out.FlushAsync();
                await outputWriter.FlushAsync();

                return new AssemblyExecutionResult
                {
                    Success = true,
                    Output = outputBuilder.ToString(),
                    ReturnValue = result,
                };
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
        catch (Exception ex)
        {
            return new AssemblyExecutionResult
            {
                Success = false,
                ErrorMessage = $"Execution error: {ex.Message}",
                Exception = ex,
            };
        }
        finally
        {
            // Unload the context to free memory
            context.Unload();

            // Wait for garbage collection to ensure unloading
            await Task.Run(
                () =>
                {
                    for (int i = 0; i < 10 && contextWeakRef.IsAlive; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                },
                cancellationToken
            );

            // Dispose streams
            compilationResult.AssemblyStream?.Dispose();
            compilationResult.SymbolsStream?.Dispose();
        }
    }

    /// <summary>
    /// Find the entry point in an assembly
    /// </summary>
    private static MethodInfo? FindEntryPoint(Assembly assembly)
    {
        // Look for Main method in Program class (traditional)
        var programType = assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "Program" || t.Name.Contains("Program"));
        if (programType != null)
        {
            var mainMethod = programType.GetMethod(
                "Main",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (mainMethod != null)
            {
                return mainMethod;
            }
        }

        // Look for top-level statements entry point
        var entryPointMethod = assembly
            .GetTypes()
            .SelectMany(t =>
                t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            )
            .FirstOrDefault(m => m.Name == "<Main>$");

        return entryPointMethod;
    }
}

/// <summary>
/// Result of assembly execution
/// </summary>
public class AssemblyExecutionResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public object? ReturnValue { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? CompilationErrors { get; set; }
    public Exception? Exception { get; set; }
}
