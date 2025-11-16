using System.Diagnostics;
using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;

namespace RoslynStone.Infrastructure.CommandHandlers;

/// <summary>
/// Handler for executing single-file C# programs using dotnet CLI
/// </summary>
public class ExecuteFileCommandHandler : ICommandHandler<ExecuteFileCommand, string>
{
    public async Task<string> HandleAsync(
        ExecuteFileCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (!File.Exists(command.FilePath))
        {
            return $"Error: File not found: {command.FilePath}";
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run-app {command.FilePath}",
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
