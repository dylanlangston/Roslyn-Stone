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
    private readonly RoslynScriptingService _scriptingService;
    private readonly IReplContextManager _contextManager;
    private readonly NuGetService _nugetService;

    public McpToolsIntegrationTests()
    {
        _scriptingService = new RoslynScriptingService();
        _contextManager = new ReplContextManager();
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
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
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
        Assert.Equal(4, resultDict["returnValue"].GetInt32());
        Assert.True(resultDict.ContainsKey("contextId"));
    }

    [Fact]
    [Trait("Feature", "Evaluation")]
    public async Task EvaluateCsharp_WithConsoleOutput_CapturesOutput()
    {
        // Arrange
        var code = "Console.WriteLine(\"Test Output\"); 42";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal(42, resultDict["returnValue"].GetInt32());
        Assert.Contains("Test Output", resultDict["output"].GetString());
    }

    [Fact]
    [Trait("Feature", "Evaluation")]
    public async Task EvaluateCsharp_CompilationError_ReturnsErrors()
    {
        // Arrange
        var code = "int x = \"not a number\";";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
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
        var result = await ReplTools.ValidateCsharp(_scriptingService, _contextManager, code);
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
        var result = await ReplTools.ValidateCsharp(_scriptingService, _contextManager, code);
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["isValid"].GetBoolean());
        Assert.NotEmpty(resultDict["issues"].EnumerateArray());
    }

    [Fact]
    [Trait("Feature", "Reset")]
    public async Task ResetRepl_ClearsState()
    {
        // Arrange
        var result1 = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "int x = 42;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var resultDict1 = TestJsonContext.DeserializeToDictionary(json1);
        var contextId = resultDict1!["contextId"].GetString();

        // Act
        ReplTools.ResetRepl(_contextManager, contextId);
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "x",
            contextId
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean()); // x should not be defined anymore
    }

    [Fact]
    [Trait("Feature", "StateManagement")]
    public async Task EvaluateCsharp_PreservesStateBetweenCalls()
    {
        // Arrange
        var service = new RoslynScriptingService(); // Fresh instance
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - First execution creates context
        var result1 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int value = 100;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = TestJsonContext.DeserializeToDictionary(json1);
        var contextId = result1Dict!["contextId"].GetString();

        // Second execution uses same context
        var result2 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "value + 50",
            contextId
        );

        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);

        // Assert
        Assert.NotNull(result1Dict);
        Assert.NotNull(result2Dict);
        Assert.True(result1Dict["success"].GetBoolean());
        Assert.True(result2Dict["success"].GetBoolean());
        Assert.Equal(150, result2Dict["returnValue"].GetInt32());
    }

    [Fact]
    [Trait("Feature", "Async")]
    public async Task EvaluateCsharp_AsyncCode_ExecutesCorrectly()
    {
        // Arrange
        var code = "await Task.Delay(10); \"completed\"";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
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
    public async Task EvaluateCsharp_InvalidContextId_ReturnsProperError()
    {
        // Arrange
        var invalidContextId = "invalid-context-id-12345";
        var code = "2 + 2";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            invalidContextId
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        // Check new error structure (errors array)
        Assert.True(resultDict.TryGetValue("errors", out var errorsElement));
        var errors = errorsElement.EnumerateArray().ToList();
        Assert.NotEmpty(errors);
        Assert.Equal("CONTEXT_NOT_FOUND", errors[0].GetProperty("code").GetString());
        Assert.Contains("not found or expired", errors[0].GetProperty("message").GetString());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task EvaluateCsharp_ExpiredContextId_ReturnsProperError()
    {
        // Arrange - Create context manager with very short timeout
        var shortTimeout = TimeSpan.FromMilliseconds(50);
        var contextMgr = new ReplContextManager(contextTimeout: shortTimeout);
        using var nugetSvc = new NuGetService();

        // Create a context and let it expire
        var result1 = await ReplTools.EvaluateCsharp(
            _scriptingService,
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
        var result2 = await ReplTools.EvaluateCsharp(
            _scriptingService,
            contextMgr,
            nugetSvc,
            "x + 1",
            contextId
        );
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);

        // Assert
        Assert.NotNull(result2Dict);
        Assert.False(result2Dict["success"].GetBoolean());

        // Check new error structure (errors array)
        Assert.True(result2Dict.TryGetValue("errors", out var errorsElement));
        var errors = errorsElement.EnumerateArray().ToList();
        Assert.NotEmpty(errors);
        Assert.Equal("CONTEXT_NOT_FOUND", errors[0].GetProperty("code").GetString());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task ValidateCsharp_InvalidContextId_ReturnsProperError()
    {
        // Arrange
        var invalidContextId = "invalid-validation-context-12345";
        var code = "int x = 42;";

        // Act
        var result = await ReplTools.ValidateCsharp(
            _scriptingService,
            _contextManager,
            code,
            invalidContextId
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["isValid"].GetBoolean());
        Assert.Equal("REPL_CONTEXT_INVALID", resultDict["error"].GetString());
        Assert.Contains("not found or expired", resultDict["message"].GetString());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task MultipleConcurrentContexts_IsolatedCorrectly()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Create two contexts with different variables
        var result1 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int value1 = 100;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = TestJsonContext.DeserializeToDictionary(json1);
        var context1Id = result1Dict!["contextId"].GetString();

        var result2 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int value2 = 200;",
            createContext: true
        );
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);
        var context2Id = result2Dict!["contextId"].GetString();

        // Use variables in their respective contexts
        var result1Use = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "value1 + 50",
            context1Id
        );
        var json1Use = JsonSerializer.Serialize(result1Use);
        var result1UseDict = TestJsonContext.DeserializeToDictionary(json1Use);

        var result2Use = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "value2 + 50",
            context2Id
        );
        var json2Use = JsonSerializer.Serialize(result2Use);
        var result2UseDict = TestJsonContext.DeserializeToDictionary(json2Use);

        // Assert - Both contexts work correctly and independently
        Assert.NotNull(result1UseDict);
        Assert.True(result1UseDict["success"].GetBoolean());
        Assert.Equal(150, result1UseDict["returnValue"].GetInt32());

        Assert.NotNull(result2UseDict);
        Assert.True(result2UseDict["success"].GetBoolean());
        Assert.Equal(250, result2UseDict["returnValue"].GetInt32());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task ContextIsolation_VariablesDoNotLeak()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Define variable in context1
        var result1 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int secretValue = 999;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = TestJsonContext.DeserializeToDictionary(json1);
        _ = result1Dict!["contextId"].GetString(); // Context 1 created but not used for validation

        // Try to access variable from context2 (should fail - creates new temporary context)
        var result2 = await ReplTools.EvaluateCsharp(service, contextMgr, nugetSvc, "secretValue");
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);

        // Assert
        Assert.NotNull(result2Dict);
        Assert.False(result2Dict["success"].GetBoolean());
        Assert.NotEmpty(result2Dict["errors"].EnumerateArray());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task ResetRepl_WithSpecificContextId_ResetsOnlyThatContext()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Create two contexts
        var result1 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int value1 = 100;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = TestJsonContext.DeserializeToDictionary(json1);
        var context1Id = result1Dict!["contextId"].GetString();

        var result2 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int value2 = 200;",
            createContext: true
        );
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);
        var context2Id = result2Dict!["contextId"].GetString();

        // Act - Reset only context1
        var resetResult = ReplTools.ResetRepl(contextMgr, context1Id);
        var resetJson = TestJsonContext.SerializeDynamic(resetResult);
        var resetDict = TestJsonContext.DeserializeToDictionary(resetJson);

        // Try to use both contexts
        var result1After = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "value1",
            context1Id
        );
        var json1After = JsonSerializer.Serialize(result1After);
        var result1AfterDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json1After
        );

        var result2After = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "value2 + 50",
            context2Id
        );
        var json2After = JsonSerializer.Serialize(result2After);
        var result2AfterDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json2After
        );

        // Assert
        Assert.NotNull(resetDict);
        Assert.True(resetDict["success"].GetBoolean());

        // Context1 should be invalid
        Assert.NotNull(result1AfterDict);
        Assert.False(result1AfterDict["success"].GetBoolean());

        // Check new error structure (errors array)
        Assert.True(result1AfterDict.TryGetValue("errors", out var errorsElement));
        var errors = errorsElement.EnumerateArray().ToList();
        Assert.NotEmpty(errors);
        Assert.Equal("CONTEXT_NOT_FOUND", errors[0].GetProperty("code").GetString());

        // Context2 should still work
        Assert.NotNull(result2AfterDict);
        Assert.True(result2AfterDict["success"].GetBoolean());
        Assert.Equal(250, result2AfterDict["returnValue"].GetInt32());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task ResetRepl_WithoutContextId_ResetsAllContexts()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Create multiple contexts
        var result1 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int x = 1;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = TestJsonContext.DeserializeToDictionary(json1);
        var context1Id = result1Dict!["contextId"].GetString();

        var result2 = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "int y = 2;",
            createContext: true
        );
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = TestJsonContext.DeserializeToDictionary(json2);
        var context2Id = result2Dict!["contextId"].GetString();

        // Act - Reset all contexts
        var resetResult = ReplTools.ResetRepl(contextMgr);
        var resetJson = TestJsonContext.SerializeDynamic(resetResult);
        var resetDict = TestJsonContext.DeserializeToDictionary(resetJson);

        // Try to use contexts
        var result1After = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "x",
            context1Id
        );
        var json1After = JsonSerializer.Serialize(result1After);
        var result1AfterDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json1After
        );

        var result2After = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "y",
            context2Id
        );
        var json2After = JsonSerializer.Serialize(result2After);
        var result2AfterDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json2After
        );

        // Assert
        Assert.NotNull(resetDict);
        Assert.True(resetDict["success"].GetBoolean());
        Assert.Equal(2, resetDict["sessionsCleared"].GetInt32());

        // Both contexts should be invalid
        Assert.False(result1AfterDict!["success"].GetBoolean());
        Assert.False(result2AfterDict!["success"].GetBoolean());
    }
}
