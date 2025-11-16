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

    public ExecuteCodeCommandHandler(RoslynScriptingService scriptingService)
    {
        _scriptingService = scriptingService;
    }

    public async Task<ExecutionResult> HandleAsync(ExecuteCodeCommand command, CancellationToken cancellationToken = default)
    {
        return await _scriptingService.ExecuteAsync(command.Code, cancellationToken);
    }
}
