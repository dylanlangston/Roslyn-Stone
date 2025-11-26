using System.Reflection;
// ReSharper disable once RedundantUsingDirective - System.Text.StringBuilder is used
using System.Text;
using RoslynStone.Infrastructure.Models;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for executing compiled assemblies with proper isolation and cleanup
/// Based on Laurent Kempé's approach using AssemblyLoadContext
/// </summary>
public class AssemblyExecutionService
{
    private readonly CompilationService _compilationService;
    private readonly SecurityConfiguration _securityConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyExecutionService"/> class
    /// </summary>
    /// <param name="compilationService">The compilation service</param>
    /// <param name="securityConfig">Optional security configuration (uses development defaults if not provided)</param>
    public AssemblyExecutionService(
        CompilationService compilationService,
        SecurityConfiguration? securityConfig = null
    )
    {
        _compilationService = compilationService;
        _securityConfig = securityConfig ?? SecurityConfiguration.CreateDevelopmentDefaults();
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
        return await ExecuteInUnloadableContextAsync(compilationResult, cancellationToken);
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

        var context = new UnloadableAssemblyLoadContext(
            _securityConfig.BlockedAssemblies,
            logger: null // AssemblyExecutionService doesn't have ILogger
        );
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
                await using var outputWriter = new StringWriter(outputBuilder);
                Console.SetOut(outputWriter);
                Console.SetError(outputWriter);

                // Execute the entry point
                var result = entryPoint.Invoke(
                    null,
                    entryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]
                );

                // Handle async Task return types with timeout
                if (result is Task task)
                {
                    if (_securityConfig.EnableExecutionTimeout)
                    {
                        var completedTask = await Task.WhenAny(
                            task,
                            Task.Delay(_securityConfig.ExecutionTimeout, cancellationToken)
                        );

                        if (completedTask != task)
                        {
                            // Timeout occurred
                            return new AssemblyExecutionResult
                            {
                                Success = false,
                                ErrorMessage =
                                    $"Code execution exceeded the timeout limit of {_securityConfig.ExecutionTimeout.TotalSeconds} seconds",
                            };
                        }
                    }

                    await task;
                }

                // Flush output after completion
                await Console.Out.FlushAsync(cancellationToken);
                await outputWriter.FlushAsync(cancellationToken);

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
        catch (InsufficientMemoryException ex)
        {
            return new AssemblyExecutionResult { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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
            try
            {
                await Task.Run(
                    () =>
                    {
                        for (int i = 0; i < 10 && contextWeakRef.IsAlive; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    },
                    CancellationToken.None // Use None to ensure cleanup happens even on timeout
                );
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Dispose streams
            if (compilationResult.AssemblyStream != null)
                await compilationResult.AssemblyStream.DisposeAsync();
            if (compilationResult.SymbolsStream != null)
                await compilationResult.SymbolsStream.DisposeAsync();
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
    /// <summary>
    /// Gets or sets a value indicating whether the execution was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the console output from execution
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets or sets the return value from the entry point
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public object? ReturnValue { get; init; }

    /// <summary>
    /// Gets or sets the error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the compilation errors if any
    /// </summary>
    public List<string>? CompilationErrors { get; init; }

    /// <summary>
    /// Gets or sets the exception that occurred during execution
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Exception? Exception { get; init; }
}
