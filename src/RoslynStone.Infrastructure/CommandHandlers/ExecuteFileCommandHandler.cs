using System.Diagnostics;
using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.CommandHandlers;

/// <summary>
/// Handler for executing single-file C# programs
/// Uses AssemblyLoadContext for proper isolation and memory management
/// Falls back to dotnet CLI if preferred
/// </summary>
public class ExecuteFileCommandHandler : ICommandHandler<ExecuteFileCommand, string>
{
    private readonly AssemblyExecutionService _assemblyExecutionService;
    private readonly bool _useDotnetCli;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteFileCommandHandler"/> class
    /// </summary>
    /// <param name="assemblyExecutionService">The assembly execution service</param>
    /// <param name="useDotnetCli">Whether to use dotnet CLI instead of AssemblyLoadContext</param>
    public ExecuteFileCommandHandler(
        AssemblyExecutionService assemblyExecutionService,
        bool useDotnetCli = false
    )
    {
        _assemblyExecutionService = assemblyExecutionService;
        _useDotnetCli = useDotnetCli;
    }

    /// <summary>
    /// Handles the execute file command
    /// </summary>
    /// <param name="command">The command containing the file path to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution output or error message</returns>
    public async Task<string> HandleAsync(
        ExecuteFileCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (!File.Exists(command.FilePath))
        {
            return $"Error: File not found: {command.FilePath}";
        }

        // Use dotnet CLI if explicitly requested
        if (_useDotnetCli)
        {
            return await ExecuteWithDotnetCliAsync(command.FilePath, cancellationToken);
        }

        // Use AssemblyLoadContext approach (preferred)
        var result = await _assemblyExecutionService.ExecuteFileAsync(
            command.FilePath,
            cancellationToken
        );

        if (!result.Success)
        {
            var errorMessage = result.ErrorMessage ?? "Unknown error";
            if (result.CompilationErrors?.Any() == true)
            {
                errorMessage += "\n\nCompilation errors:\n" + string.Join("\n", result.CompilationErrors);
            }
            return $"Error: {errorMessage}";
        }

        return result.Output ?? string.Empty;
    }

    /// <summary>
    /// Execute file using dotnet CLI (fallback method)
    /// </summary>
    private async Task<string> ExecuteWithDotnetCliAsync(
        string filePath,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run-app {filePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return $"Error (exit code {process.ExitCode}):\n{error}";
            }

            return output;
        }
        catch (Exception ex)
        {
            return $"Error executing file: {ex.Message}";
        }
    }
}
