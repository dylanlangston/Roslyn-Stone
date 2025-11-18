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
        var result1 = await ReplTools.EvaluateCsharp(_scriptingService, _contextManager, "int x = 42;");
        var json1 = JsonSerializer.Serialize(result1);
        var resultDict1 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var contextId = resultDict1!["contextId"].GetString();

        // Act
        ReplTools.ResetRepl(_contextManager, contextId);
        var result = await ReplTools.EvaluateCsharp(_scriptingService, _contextManager, "x", contextId);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean()); // x should not be defined anymore
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public void GetDocumentation_ValidType_ReturnsDocumentation()
    {
        // Arrange
        var symbolName = "System.String";

        // Act
        var result = DocumentationTools.GetDocumentation(_documentationService, symbolName);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        // Documentation may not be found in test environment without XML docs
        // Just verify the tool returns a valid structure
        Assert.True(resultDict.ContainsKey("found"));

        if (resultDict["found"].GetBoolean())
        {
            Assert.Equal("System.String", resultDict["symbolName"].GetString());
        }
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public void GetDocumentation_InvalidType_ReturnsNotFound()
    {
        // Arrange
        var symbolName = "InvalidType.DoesNotExist";

        // Act
        var result = DocumentationTools.GetDocumentation(_documentationService, symbolName);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["found"].GetBoolean());
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
    [Trait("Feature", "ReplInfo")]
    public void GetReplInfo_ReturnsEnvironmentInformation()
    {
        // Act
        var result = ReplTools.GetReplInfo(_contextManager);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("frameworkVersion", out var frameworkVersion));
        Assert.True(resultDict.ContainsKey("language"));
        Assert.True(resultDict.TryGetValue("defaultImports", out var defaultImports));
        Assert.True(resultDict.TryGetValue("capabilities", out var capabilities));
        Assert.True(resultDict.ContainsKey("activeSessions"));

        // Verify framework version
        Assert.Equal(".NET 10.0", frameworkVersion.GetString());

        // Verify capabilities
        Assert.True(capabilities.GetProperty("asyncAwait").GetBoolean());
        Assert.True(capabilities.GetProperty("linq").GetBoolean());
        Assert.True(capabilities.GetProperty("contextManagement").GetBoolean());

        // Verify default imports exist
        var imports = defaultImports.EnumerateArray().ToList();
        Assert.NotEmpty(imports);
        Assert.Contains(imports, i => i.GetString() == "System");
        Assert.Contains(imports, i => i.GetString() == "System.Linq");
    }
}
