using Microsoft.AspNetCore.Mvc;
using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;

namespace RoslynStone.Api.Controllers;

/// <summary>
/// REPL controller for executing C# code
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReplController : ControllerBase
{
    private readonly ICommandHandler<ExecuteCodeCommand, ExecutionResult> _executeHandler;
    private readonly IQueryHandler<ValidateCodeQuery, IReadOnlyList<CompilationError>> _validateHandler;

    public ReplController(
        ICommandHandler<ExecuteCodeCommand, ExecutionResult> executeHandler,
        IQueryHandler<ValidateCodeQuery, IReadOnlyList<CompilationError>> validateHandler)
    {
        _executeHandler = executeHandler;
        _validateHandler = validateHandler;
    }

    /// <summary>
    /// Execute C# code in the REPL
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<ExecutionResult>> Execute([FromBody] ExecuteRequest request)
    {
        var command = new ExecuteCodeCommand(request.Code);
        var result = await _executeHandler.HandleAsync(command);
        return Ok(result);
    }

    /// <summary>
    /// Validate C# code without executing it
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<IReadOnlyList<CompilationError>>> Validate([FromBody] ValidateRequest request)
    {
        var query = new ValidateCodeQuery(request.Code);
        var result = await _validateHandler.HandleAsync(query);
        return Ok(result);
    }
}

public record ExecuteRequest(string Code);
public record ValidateRequest(string Code);
