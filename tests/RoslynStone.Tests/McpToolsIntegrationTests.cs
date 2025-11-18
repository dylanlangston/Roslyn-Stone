using System.Text.Json;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;

namespace RoslynStone.Tests;

/// <summary>
/// Integration tests for MCP tools
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "MCP")]
public class McpToolsIntegrationTests
{
    private readonly RoslynScriptingService _scriptingService;
    private readonly DocumentationService _documentationService;
    private readonly IReplContextManager _contextManager;

    public McpToolsIntegrationTests()
    {
        _scriptingService = new RoslynScriptingService();
        _documentationService = new DocumentationService();
        _contextManager = new ReplContextManager();
    }

    [Fact]
    [Trait("Feature", "Evaluation")]
    public async Task EvaluateCsharp_SimpleExpression_ReturnsCorrectResult()
    {
        // Arrange
        var code = "2 + 2";

        // Act
        var result = await ReplTools.EvaluateCsharp(_scriptingService, _contextManager, code);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
        var result = await ReplTools.EvaluateCsharp(_scriptingService, _contextManager, code);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
        var result = await ReplTools.EvaluateCsharp(_scriptingService, _contextManager, code);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
            "int x = 42;"
        );
        var json1 = JsonSerializer.Serialize(result1);
        var resultDict1 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var contextId = resultDict1!["contextId"].GetString();

        // Act
        ReplTools.ResetRepl(_contextManager, contextId);
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            "x",
            contextId
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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

        // Act - First execution creates context
        var result1 = await ReplTools.EvaluateCsharp(service, contextMgr, "int value = 100;");
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var contextId = result1Dict!["contextId"].GetString();

        // Second execution uses same context
        var result2 = await ReplTools.EvaluateCsharp(service, contextMgr, "value + 50", contextId);

        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2);

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
        var result = await ReplTools.EvaluateCsharp(_scriptingService, _contextManager, code);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
            code,
            invalidContextId
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());
        Assert.Equal("REPL_CONTEXT_INVALID", resultDict["error"].GetString());
        Assert.Contains("not found or expired", resultDict["message"].GetString());
        Assert.Contains("contextId", resultDict["suggestedAction"].GetString());
    }

    [Fact]
    [Trait("Feature", "ContextManagement")]
    public async Task EvaluateCsharp_ExpiredContextId_ReturnsProperError()
    {
        // Arrange - Create context manager with very short timeout
        var shortTimeout = TimeSpan.FromMilliseconds(50);
        var contextMgr = new ReplContextManager(contextTimeout: shortTimeout);

        // Create a context and let it expire
        var result1 = await ReplTools.EvaluateCsharp(_scriptingService, contextMgr, "int x = 42;");
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var contextId = result1Dict!["contextId"].GetString();

        // Wait for context to expire
        await Task.Delay(100);
        contextMgr.CleanupExpiredContexts();

        // Act - Try to use expired context
        var result2 = await ReplTools.EvaluateCsharp(
            _scriptingService,
            contextMgr,
            "x + 1",
            contextId
        );
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2);

        // Assert
        Assert.NotNull(result2Dict);
        Assert.False(result2Dict["success"].GetBoolean());
        Assert.Equal("REPL_CONTEXT_INVALID", result2Dict["error"].GetString());
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
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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

        // Act - Create two contexts with different variables
        var result1 = await ReplTools.EvaluateCsharp(service, contextMgr, "int value1 = 100;");
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var context1Id = result1Dict!["contextId"].GetString();

        var result2 = await ReplTools.EvaluateCsharp(service, contextMgr, "int value2 = 200;");
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2);
        var context2Id = result2Dict!["contextId"].GetString();

        // Use variables in their respective contexts
        var result1Use = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            "value1 + 50",
            context1Id
        );
        var json1Use = JsonSerializer.Serialize(result1Use);
        var result1UseDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1Use);

        var result2Use = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            "value2 + 50",
            context2Id
        );
        var json2Use = JsonSerializer.Serialize(result2Use);
        var result2UseDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2Use);

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

        // Act - Define variable in context1
        var result1 = await ReplTools.EvaluateCsharp(service, contextMgr, "int secretValue = 999;");
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        _ = result1Dict!["contextId"].GetString(); // Context 1 created but not used for validation

        // Try to access variable from context2 (should fail)
        var result2 = await ReplTools.EvaluateCsharp(service, contextMgr, "secretValue");
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2);

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

        // Create two contexts
        var result1 = await ReplTools.EvaluateCsharp(service, contextMgr, "int value1 = 100;");
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var context1Id = result1Dict!["contextId"].GetString();

        var result2 = await ReplTools.EvaluateCsharp(service, contextMgr, "int value2 = 200;");
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2);
        var context2Id = result2Dict!["contextId"].GetString();

        // Act - Reset only context1
        var resetResult = ReplTools.ResetRepl(contextMgr, context1Id);
        var resetJson = JsonSerializer.Serialize(resetResult);
        var resetDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resetJson);

        // Try to use both contexts
        var result1After = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
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
        Assert.Equal("REPL_CONTEXT_INVALID", result1AfterDict["error"].GetString());

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

        // Create multiple contexts
        var result1 = await ReplTools.EvaluateCsharp(service, contextMgr, "int x = 1;");
        var json1 = JsonSerializer.Serialize(result1);
        var result1Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var context1Id = result1Dict!["contextId"].GetString();

        var result2 = await ReplTools.EvaluateCsharp(service, contextMgr, "int y = 2;");
        var json2 = JsonSerializer.Serialize(result2);
        var result2Dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2);
        var context2Id = result2Dict!["contextId"].GetString();

        // Act - Reset all contexts
        var resetResult = ReplTools.ResetRepl(contextMgr);
        var resetJson = JsonSerializer.Serialize(resetResult);
        var resetDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resetJson);

        // Try to use contexts
        var result1After = await ReplTools.EvaluateCsharp(service, contextMgr, "x", context1Id);
        var json1After = JsonSerializer.Serialize(result1After);
        var result1AfterDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json1After
        );

        var result2After = await ReplTools.EvaluateCsharp(service, contextMgr, "y", context2Id);
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
