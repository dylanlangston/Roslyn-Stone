using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.CommandHandlers;

/// <summary>
/// Handler for executing C# code
/// </summary>
public class ExecuteCodeCommandHandler : ICommandHandler<ExecuteCodeCommand, ExecutionResult>
{
    private readonly RoslynScriptingService _scriptingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteCodeCommandHandler"/> class
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    public ExecuteCodeCommandHandler(RoslynScriptingService scriptingService)
    {
        _scriptingService = scriptingService;
    }

    /// <summary>
    /// Handles the execute code command
    /// </summary>
    /// <param name="command">The command containing the code to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution result</returns>
    public async Task<ExecutionResult> HandleAsync(
        ExecuteCodeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        return await _scriptingService.ExecuteAsync(command.Code, cancellationToken);
    }
}
