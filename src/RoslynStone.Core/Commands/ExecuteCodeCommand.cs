using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;

namespace RoslynStone.Core.Commands;

/// <summary>
/// Command to execute C# code in the REPL
/// </summary>
public record ExecuteCodeCommand(string Code) : ICommand<ExecutionResult>;
