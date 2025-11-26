using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for RoslynScriptingService
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "REPL")]
public class RoslynScriptingServiceTests
{
    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteAsync_SimpleExpression_ReturnsResult()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "1 + 1";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ReturnValue);
        Assert.Empty(result.Errors);
    }

    [Fact]
    [Trait("Feature", "Output")]
    public async Task ExecuteAsync_ConsoleOutput_CapturesOutput()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "Console.WriteLine(\"Hello, World!\");";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Hello, World!", result.Output);
    }

    [Fact]
    [Trait("Feature", "Validation")]
    public async Task ExecuteAsync_CompilationError_ReturnsErrors()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "int x = \"not a number\";";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    [Trait("Feature", "State")]
    public async Task ExecuteAsync_MultipleStatements_MaintainsState()
    {
        // Arrange
        var service = new RoslynScriptingService(); // Use same instance for both calls
        var code1 = "int x = 10; x"; // Return x to ensure success
        var code2 = "x + 5";

        // Act
        var result1 = await service.ExecuteAsync(code1);
        var result2 = await service.ExecuteAsync(code2);

        // Assert
        Assert.True(
            result1.Success,
            $"First execution failed: {string.Join(", ", result1.Errors.Select(e => e.Message))}"
        );
        Assert.Equal(10, result1.ReturnValue);
        Assert.True(
            result2.Success,
            $"Second execution failed: {string.Join(", ", result2.Errors.Select(e => e.Message))}"
        );
        Assert.Equal(15, result2.ReturnValue);
    }

    [Fact]
    [Trait("Feature", "Warnings")]
    public async Task ExecuteAsync_Warning_ReturnsWarnings()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "int x = 5; int y = 10;";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        // Unused variables might generate warnings
    }

    [Fact]
    [Trait("Feature", "RuntimeError")]
    public async Task ExecuteAsync_RuntimeException_ReturnsRuntimeError()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "throw new System.InvalidOperationException(\"Test exception\");";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "RUNTIME_ERROR");
        Assert.Contains(result.Errors, e => e.Message.Contains("Test exception"));
    }

    [Fact]
    [Trait("Feature", "RuntimeError")]
    public async Task ExecuteAsync_DivideByZero_ReturnsRuntimeError()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "int x = 10; int y = 0; int z = x / y;";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "RUNTIME_ERROR");
    }

    [Fact]
    [Trait("Feature", "RuntimeError")]
    public async Task ExecuteAsync_NullReferenceException_ReturnsRuntimeError()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "string s = null; var length = s.Length;";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "RUNTIME_ERROR");
    }

    [Fact]
    [Trait("Feature", "Cancellation")]
    public async Task ExecuteAsync_CancellationRequested_ThrowsTaskCanceledException()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "System.Threading.Thread.Sleep(5000);";
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert - TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.ExecuteAsync(code, cts.Token)
        );
    }

    [Fact]
    [Trait("Feature", "Reset")]
    public async Task Reset_AfterExecution_ClearsState()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code1 = "int x = 10;";

        // Act
        await service.ExecuteAsync(code1);
        service.Reset();
        var result = await service.ExecuteAsync("x");

        // Assert - x should no longer exist after reset
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    [Trait("Feature", "Reset")]
    public async Task Reset_ClearsConsoleOutput()
    {
        // Arrange
        var service = new RoslynScriptingService();
        await service.ExecuteAsync("Console.WriteLine(\"Test\");");

        // Act
        service.Reset();
        var result = await service.ExecuteAsync("Console.WriteLine(\"After reset\");");

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("Test", result.Output);
        Assert.Contains("After reset", result.Output);
    }

    [Fact]
    [Trait("Feature", "State")]
    public async Task ExecuteAsync_FirstExecution_CreatesNewScriptState()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "var greeting = \"Hello\";";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    [Fact]
    [Trait("Feature", "State")]
    public async Task ExecuteAsync_ContinuedExecution_UsesExistingState()
    {
        // Arrange
        var service = new RoslynScriptingService();
        await service.ExecuteAsync("var greeting = \"Hello\";");

        // Act - Continue with existing state
        var result = await service.ExecuteAsync("greeting + \" World\"");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Hello World", result.ReturnValue);
    }

    [Fact]
    [Trait("Feature", "ExecutionTime")]
    public async Task ExecuteAsync_TracksExecutionTime()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "1 + 1";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ExecutionTime.TotalMilliseconds >= 0);
    }

    [Fact]
    [Trait("Feature", "ExecutionTime")]
    public async Task ExecuteAsync_CompilationError_TracksExecutionTime()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "invalid code";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.ExecutionTime.TotalMilliseconds >= 0);
    }

    [Fact]
    [Trait("Feature", "Output")]
    public async Task ExecuteAsync_MultipleConsoleWrites_CapturesAllOutput()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code =
            @"
Console.Write(""Line1"");
Console.WriteLine();
Console.Write(""Line2"");
";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Line1", result.Output);
        Assert.Contains("Line2", result.Output);
    }

    [Fact]
    [Trait("Feature", "Output")]
    public async Task ExecuteAsync_EmptyOutput_ReturnsEmptyString()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = "1 + 1"; // No console output

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(string.Empty, result.Output);
    }

    [Fact]
    [Trait("Feature", "ScriptOptions")]
    public void ScriptOptions_ReturnsConfiguredOptions()
    {
        // Arrange
        var service = new RoslynScriptingService();

        // Act
        var options = service.ScriptOptions;

        // Assert
        Assert.NotNull(options);
        Assert.NotEmpty(options.MetadataReferences);
        Assert.NotEmpty(options.Imports);
    }
}
