using System.Text.Json;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;
using RoslynStone.Tests.Serialization;

namespace RoslynStone.Tests;

/// <summary>
/// Integration tests for MCP tools
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "MCP")]
public class McpToolsIntegrationTests : IDisposable
{
    private readonly IExecutionContextManager _contextManager;
    private readonly NuGetService _nugetService;

    public McpToolsIntegrationTests()
    {
        _contextManager = new ExecutionContextManager();
        _nugetService = new NuGetService();
    }

    public void Dispose()
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        _nugetService?.Dispose();
    }

    [Fact]
    [Trait("Feature", "Evaluation")]
    public async Task EvaluateCsharp_SimpleExpression_ReturnsCorrectResult()
    {
        // Arrange
        var code = "2 + 2";

        // Act - Using createContext: true to maintain stateful behavior
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code,
            createContext: true
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal("4", resultDict["returnValue"].GetString());
        Assert.True(resultDict.ContainsKey("contextId"));
    }

    [Fact]
    [Trait("Feature", "Evaluation")]
    public async Task EvaluateCsharp_WithConsoleOutput_CapturesOutput()
    {
        // Arrange
        var code = "Console.WriteLine(\"Test Output\"); 42";

        // Act
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        // In isolated execution, ALL output (Console.WriteLine + final expression) goes to returnValue
        var returnValue = resultDict["returnValue"].GetString();
        Assert.Contains("Test Output", returnValue);
        Assert.Contains("42", returnValue);
    }

    [Fact]
    [Trait("Feature", "Evaluation")]
    public async Task EvaluateCsharp_CompilationError_ReturnsErrors()
    {
        // Arrange
        var code = "int x = \"not a number\";";

        // Act
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());
        Assert.NotEmpty(resultDict["errors"].EnumerateArray());
    }

    [Fact]
    [Trait("Feature", "Validation")]
    public async Task ValidateCsharp_ValidCode_ReturnsValid()
    {
        // Arrange
        var code = "int x = 42;";

        // Act
        var result = await FileBasedToolsTestHelpers.ValidateCsharpTest(
            _contextManager,
            _nugetService,
            code
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["isValid"].GetBoolean());
    }

    [Fact]
    [Trait("Feature", "Validation")]
    public async Task ValidateCsharp_InvalidCode_ReturnsErrors()
    {
        // Arrange
        var code = "int x = \"not a number\";";

        // Act
        var result = await FileBasedToolsTestHelpers.ValidateCsharpTest(
            _contextManager,
            _nugetService,
            code
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["isValid"].GetBoolean());
        Assert.NotEmpty(resultDict["issues"].EnumerateArray());
    }

    [Fact]
    [Trait("Feature", "Async")]
    public async Task EvaluateCsharp_AsyncCode_ExecutesCorrectly()
    {
        // Arrange
        var code = "await Task.Delay(10); \"completed\"";

        // Act
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal("completed", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task EvaluateCsharp_ExpiredContextId_ReturnsProperError()
    {
        // Arrange - Create context manager with very short timeout
        var shortTimeout = TimeSpan.FromMilliseconds(50);
        var contextMgr = new ExecutionContextManager(contextTimeout: shortTimeout);
        using var nugetSvc = new NuGetService();

        // Create a context and let it expire
        var result1 = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            "int x = 42;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = TestJsonContext.DeserializeToDictionary(json1);
        var contextId = result1Dict!["contextId"].GetString();

        // Wait for context to expire
        await Task.Delay(100);
        contextMgr.CleanupExpiredContexts();

        // Act - Try to use expired context
        var result2 = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            "x + 1",
            contextId
        );
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);

        // Assert - In isolated execution, expired context means variable 'x' doesn't exist
        Assert.NotNull(result2Dict);
        Assert.False(result2Dict["success"].GetBoolean());

        // Check new error structure (errors array)
        Assert.True(result2Dict.TryGetValue("errors", out var errorsElement));
        var errors = errorsElement.EnumerateArray().ToList();
        Assert.NotEmpty(errors);
        // In isolated execution, this will be a compilation error (CS0103: name 'x' does not exist)
        // since there's no state to look up
        Assert.Contains("CS", errors[0].GetProperty("code").GetString());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task ContextIsolation_VariablesDoNotLeak()
    {
        // Arrange
        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Define variable in context1
        var result1 = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            "int secretValue = 999;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = TestJsonContext.DeserializeToDictionary(json1);
        _ = result1Dict!["contextId"].GetString(); // Context 1 created but not used for validation

        // Try to access variable from context2 (should fail - creates new temporary context)
        var result2 = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            "secretValue"
        );
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);

        // Assert
        Assert.NotNull(result2Dict);
        Assert.False(result2Dict["success"].GetBoolean());
        Assert.NotEmpty(result2Dict["errors"].EnumerateArray());
    }
}
