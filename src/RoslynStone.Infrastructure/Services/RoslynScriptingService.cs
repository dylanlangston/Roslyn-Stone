using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Functional;
using RoslynStone.Infrastructure.Helpers;
using RoslynStone.Infrastructure.Models;

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
    // ReSharper disable once InconsistentNaming - Intentionally private naming for internal implementation
    private static readonly SemaphoreSlim _executionLock = new(1, 1);

    private ScriptState? _scriptState;
    private ScriptOptions _scriptOptions;
    private readonly StringWriter _outputWriter;
    private readonly SemaphoreSlim _stateLock = new(1, 1); // Protects instance state
    private readonly SecurityConfiguration _securityConfig;
    private readonly FileSystemSecurityService _fileSystemSecurity;

    /// <summary>
    /// Gets the script options used for compilation
    /// </summary>
    public ScriptOptions ScriptOptions => _scriptOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynScriptingService"/> class
    /// </summary>
    /// <param name="securityConfig">Optional security configuration (uses development defaults if not provided)</param>
    public RoslynScriptingService(SecurityConfiguration? securityConfig = null)
    {
        _outputWriter = new StringWriter();
        _scriptOptions = MetadataReferenceHelper.GetDefaultScriptOptions();
        _securityConfig = securityConfig ?? SecurityConfiguration.CreateDevelopmentDefaults();
        _fileSystemSecurity = new FileSystemSecurityService(
            _securityConfig.EnableFilesystemRestrictions,
            _securityConfig.AllowedFilesystemPaths
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
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var stopwatch = Stopwatch.StartNew();
        var errors = new List<CompilationError>();
        var warnings = new List<CompilationError>();

        // Validate filesystem access if enabled
        if (_securityConfig.EnableFilesystemRestrictions)
        {
            var validationResult = _fileSystemSecurity.ValidateCode(code);
            if (!validationResult.IsValid)
            {
                stopwatch.Stop();
                return new ExecutionResult
                {
                    Success = false,
                    Errors = validationResult
                        .Issues.Select(issue => new CompilationError
                        {
                            Code = "FILESYSTEM_ACCESS_DENIED",
                            Message = issue,
                            Severity = "Error",
                        })
                        .ToList(),
                    ExecutionTime = stopwatch.Elapsed,
                };
            }
        }

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
                // Execute with timeout if enabled
                Task<ScriptState<object>> executionTask;

                if (_scriptState == null)
                {
                    executionTask = CSharpScript.RunAsync(
                        code,
                        _scriptOptions,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    executionTask = _scriptState.ContinueWithAsync(
                        code,
                        cancellationToken: cancellationToken
                    );
                }

                // Apply timeout wrapper if enabled
                if (_securityConfig.EnableExecutionTimeout)
                {
                    var completedTask = await Task.WhenAny(
                        executionTask,
                        Task.Delay(_securityConfig.ExecutionTimeout, cancellationToken)
                    );

                    if (completedTask != executionTask)
                    {
                        // Timeout occurred
                        stopwatch.Stop();
                        return new ExecutionResult
                        {
                            Success = false,
                            Errors =
                            [
                                new CompilationError
                                {
                                    Code = "EXECUTION_TIMEOUT",
                                    Message =
                                        $"Code execution exceeded the timeout limit of {_securityConfig.ExecutionTimeout.TotalSeconds} seconds",
                                    Severity = "Error",
                                },
                            ],
                            ExecutionTime = stopwatch.Elapsed,
                        };
                    }
                }

                _scriptState = await executionTask;
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
        catch (InsufficientMemoryException ex)
        {
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "MEMORY_LIMIT_EXCEEDED",
                        Message = ex.Message,
                        Severity = "Error",
                    },
                ],
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
    /// <remarks>
    /// <para><strong>⚠️ SECURITY WARNING - INTERNAL USE ONLY</strong></para>
    /// <para>This method modifies singleton instance state, affecting ALL users when RoslynScriptingService is registered as singleton.</para>
    /// <para><strong>DO NOT EXPOSE via MCP tools.</strong> Only safe for single-user scenarios or when service is Scoped.</para>
    /// <para><strong>For multi-tenant systems:</strong> Use context-specific options via ReplContextManager with nugetPackages parameter instead.</para>
    /// </remarks>
    [Obsolete(
        "This method modifies singleton state (CWE-668). For multi-tenant systems, use ReplContextManager with context-specific ScriptOptions instead. Do not expose via MCP tools.",
        error: false
    )]
    internal async Task AddPackageReferenceAsync(
        string packageName,
        string? version = null,
        IReadOnlyList<string>? assemblyPaths = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        await _stateLock.WaitAsync();
        try
        {
            if (assemblyPaths != null && assemblyPaths.Count > 0)
            {
                // Add each assembly to script options using MetadataReference
                // This avoids loading assemblies into the runtime (Assembly.LoadFrom)
                foreach (var assemblyPath in assemblyPaths.Where(File.Exists))
                {
                    _scriptOptions = _scriptOptions.AddReferences(
                        MetadataReference.CreateFromFile(assemblyPath)
                    );
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
    /// <param name="customOptions">Custom script options to use (overrides default options)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Execution result with return value, output, errors, timing information, and updated script state</returns>
    public async Task<ExecutionResult> ExecuteWithStateAsync(
        string code,
        ScriptState? existingState,
        ScriptOptions? customOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var stopwatch = Stopwatch.StartNew();
        var errors = new List<CompilationError>();
        var warnings = new List<CompilationError>();

        // Validate filesystem access if enabled
        if (_securityConfig.EnableFilesystemRestrictions)
        {
            var validationResult = _fileSystemSecurity.ValidateCode(code);
            if (!validationResult.IsValid)
            {
                stopwatch.Stop();
                return new ExecutionResult
                {
                    Success = false,
                    Errors = validationResult
                        .Issues.Select(issue => new CompilationError
                        {
                            Code = "FILESYSTEM_ACCESS_DENIED",
                            Message = issue,
                            Severity = "Error",
                        })
                        .ToList(),
                    ExecutionTime = stopwatch.Elapsed,
                };
            }
        }

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
                var optionsToUse = customOptions ?? _scriptOptions;
                Task<ScriptState<object>> executionTask;

                if (existingState == null)
                {
                    executionTask = CSharpScript.RunAsync(
                        code,
                        optionsToUse,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    executionTask = existingState.ContinueWithAsync(
                        code,
                        cancellationToken: cancellationToken
                    );
                }

                // Apply timeout wrapper if enabled
                ScriptState<object> newState;
                if (_securityConfig.EnableExecutionTimeout)
                {
                    var completedTask = await Task.WhenAny(
                        executionTask,
                        Task.Delay(_securityConfig.ExecutionTimeout, cancellationToken)
                    );

                    if (completedTask != executionTask)
                    {
                        // Timeout occurred
                        stopwatch.Stop();
                        return new ExecutionResult
                        {
                            Success = false,
                            Errors =
                            [
                                new CompilationError
                                {
                                    Code = "EXECUTION_TIMEOUT",
                                    Message =
                                        $"Code execution exceeded the timeout limit of {_securityConfig.ExecutionTimeout.TotalSeconds} seconds",
                                    Severity = "Error",
                                },
                            ],
                            ExecutionTime = stopwatch.Elapsed,
                            ScriptState = existingState, // Keep existing state on timeout
                        };
                    }
                }

                newState = await executionTask;
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
        catch (InsufficientMemoryException ex)
        {
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "MEMORY_LIMIT_EXCEEDED",
                        Message = ex.Message,
                        Severity = "Error",
                    },
                ],
                ExecutionTime = stopwatch.Elapsed,
                ScriptState = existingState, // Keep existing state on memory limit
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
