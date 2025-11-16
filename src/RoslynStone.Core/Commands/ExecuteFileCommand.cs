using RoslynStone.Core.CQRS;

namespace RoslynStone.Core.Commands;

/// <summary>
/// Command to execute a single-file C# program
/// </summary>
public record ExecuteFileCommand(string FilePath) : ICommand<string>;
