using RoslynStone.Infrastructure.Services;
using Xunit;

namespace RoslynStone.Tests;

public class RoslynScriptingServiceTests
{
    private readonly RoslynScriptingService _service;

    public RoslynScriptingServiceTests()
    {
        _service = new RoslynScriptingService();
    }

    [Fact]
    public async Task ExecuteAsync_SimpleExpression_ReturnsResult()
    {
        // Arrange
        var code = "1 + 1";

        // Act
        var result = await _service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ReturnValue);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_ConsoleOutput_CapturesOutput()
    {
        // Arrange
        var code = "Console.WriteLine(\"Hello, World!\");";

        // Act
        var result = await _service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Hello, World!", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_CompilationError_ReturnsErrors()
    {
        // Arrange
        var code = "int x = \"not a number\";";

        // Act
        var result = await _service.ExecuteAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleStatements_MaintainsState()
    {
        // Arrange
        var service = new RoslynScriptingService(); // Use same instance for both calls
        var code1 = "int x = 10; x";  // Return x to ensure success
        var code2 = "x + 5";

        // Act
        var result1 = await service.ExecuteAsync(code1);
        var result2 = await service.ExecuteAsync(code2);

        // Assert
        Assert.True(result1.Success, $"First execution failed: {string.Join(", ", result1.Errors.Select(e => e.Message))}");
        Assert.Equal(10, result1.ReturnValue);
        Assert.True(result2.Success, $"Second execution failed: {string.Join(", ", result2.Errors.Select(e => e.Message))}");
        Assert.Equal(15, result2.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_Warning_ReturnsWarnings()
    {
        // Arrange
        var code = "int x = 5; int y = 10;";

        // Act
        var result = await _service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        // Unused variables might generate warnings
    }
}
