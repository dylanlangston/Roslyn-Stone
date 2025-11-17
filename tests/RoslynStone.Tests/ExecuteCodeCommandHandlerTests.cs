using RoslynStone.Core.Commands;
using RoslynStone.Infrastructure.CommandHandlers;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for ExecuteCodeCommandHandler
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "CommandHandler")]
public class ExecuteCodeCommandHandlerTests
{
    private readonly ExecuteCodeCommandHandler _handler;

    public ExecuteCodeCommandHandlerTests()
    {
        var service = new RoslynScriptingService();
        _handler = new ExecuteCodeCommandHandler(service);
    }

    [Fact]
    [Trait("Feature", "Execution")]
    public async Task HandleAsync_ValidCode_ExecutesSuccessfully()
    {
        // Arrange
        var command = new ExecuteCodeCommand("2 + 2");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4, result.ReturnValue);
    }

    [Fact]
    [Trait("Feature", "Validation")]
    public async Task HandleAsync_InvalidCode_ReturnsErrors()
    {
        // Arrange
        var command = new ExecuteCodeCommand("this is not valid C# code !!!");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }
}
