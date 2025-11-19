using System.Text.Json;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for REPL tools context control parameter functionality
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "REPL")]
public class ReplToolsContextControlTests : IDisposable
{
    private readonly RoslynScriptingService _scriptingService;
    private readonly IReplContextManager _contextManager;
    private readonly NuGetService _nugetService;

    public ReplToolsContextControlTests()
    {
        _scriptingService = new RoslynScriptingService();
        _contextManager = new ReplContextManager();
        _nugetService = new NuGetService();
    }

    public void Dispose()
    {
        _nugetService?.Dispose();
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task EvaluateCsharp_CreateContextFalse_DoesNotReturnContextId()
    {
        // Arrange
        var code = "1 + 1";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            createContext: false
        );

        // Serialize to inspect result
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal(2, resultDict["returnValue"].GetInt32());
        Assert.False(
            resultDict.TryGetValue("contextId", out var contextIdElem)
                && contextIdElem.ValueKind != JsonValueKind.Null
        );
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task EvaluateCsharp_CreateContextTrue_ReturnsContextId()
    {
        // Arrange
        var code = "var x = 42;";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            createContext: true
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.True(resultDict.TryGetValue("contextId", out var contextIdElement));
        Assert.NotNull(contextIdElement.GetString());
        Assert.True(Guid.TryParse(contextIdElement.GetString(), out _));
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task EvaluateCsharp_DefaultBehavior_DoesNotReturnContextId()
    {
        // Arrange
        var code = "1 + 1";

        // Act - Call without createContext parameter (should default to false)
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.False(
            resultDict.TryGetValue("contextId", out var contextIdElem)
                && contextIdElem.ValueKind != JsonValueKind.Null
        );
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task EvaluateCsharp_WithContextId_IgnoresCreateContextParameter()
    {
        // Arrange - First create a context
        var createResult = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "var x = 10;",
            createContext: true
        );
        var json1 = JsonSerializer.Serialize(createResult);
        var dict1 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var contextId = dict1!["contextId"].GetString();

        // Act - Continue with the context (createContext should be ignored)
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "x * 2",
            contextId: contextId,
            createContext: false // This should be ignored since contextId is provided
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal(20, resultDict["returnValue"].GetInt32());
        Assert.Equal(contextId, resultDict["contextId"].GetString());
    }

    [Fact]
    [Trait("Feature", "NuGetIntegration")]
    public async Task EvaluateCsharp_WithNuGetPackages_LoadsPackagesBeforeExecution()
    {
        // Arrange
        var code =
            @"
using Newtonsoft.Json;
var obj = new { Name = ""Test"", Value = 42 };
JsonConvert.SerializeObject(obj)
";
        var packages = new[] { new NuGetPackageSpec { PackageName = "Newtonsoft.Json" } };

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            nugetPackages: packages,
            createContext: false
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Contains("Test", resultDict["returnValue"].GetString());
        Assert.Contains("42", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "NuGetIntegration")]
    public async Task EvaluateCsharp_WithNuGetPackagesAndVersion_LoadsSpecificVersion()
    {
        // Arrange
        var code =
            @"
using Newtonsoft.Json;
JsonConvert.SerializeObject(new { Test = 1 })
";
        var packages = new[]
        {
            new NuGetPackageSpec { PackageName = "Newtonsoft.Json", Version = "13.0.1" },
        };

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            nugetPackages: packages,
            createContext: false
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Contains("Test", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "NuGetIntegration")]
    public async Task EvaluateCsharp_WithNullNuGetPackages_ExecutesWithoutPackageLoading()
    {
        // Arrange
        var code = "1 + 1";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            nugetPackages: null,
            createContext: false
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal(2, resultDict["returnValue"].GetInt32());
    }

    [Fact]
    [Trait("Feature", "NuGetIntegration")]
    public async Task EvaluateCsharp_WithEmptyNuGetPackages_ExecutesWithoutPackageLoading()
    {
        // Arrange
        var code = "1 + 1";

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            nugetPackages: Array.Empty<NuGetPackageSpec>(),
            createContext: false
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal(2, resultDict["returnValue"].GetInt32());
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task EvaluateCsharp_TemporaryContext_VariablesNotPersisted()
    {
        // Arrange & Act - Execute code in temporary context
        await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "var x = 10;",
            createContext: false
        );

        // Try to access variable from previous execution
        var result2 = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "x * 2",
            createContext: false
        );

        var json = JsonSerializer.Serialize(result2);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert - Should fail because x is not defined
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());
        Assert.True(resultDict["errors"].GetArrayLength() > 0);
    }

    [Fact]
    [Trait("Feature", "NuGetIntegration")]
    public async Task EvaluateCsharp_WithMultiplePackages_LoadsAllPackages()
    {
        // Arrange
        var code =
            @"
using Newtonsoft.Json;
var data = new { X = 10, Y = 20 };
JsonConvert.SerializeObject(data)
";
        var packages = new[]
        {
            new NuGetPackageSpec { PackageName = "Newtonsoft.Json" },
            new NuGetPackageSpec { PackageName = "System.Text.Json" },
        };

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code,
            nugetPackages: packages,
            createContext: false
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Contains("10", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "NuGetIntegration")]
    public async Task EvaluateCsharp_WithStatefulContextAndPackages_PackagesPersistInContext()
    {
        // Arrange - Load package in stateful context
        var packages = new[] { new NuGetPackageSpec { PackageName = "Newtonsoft.Json" } };

        var result1 = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "using Newtonsoft.Json;",
            nugetPackages: packages,
            createContext: true
        );

        var json1 = JsonSerializer.Serialize(result1);
        var dict1 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json1);
        var contextId = dict1!["contextId"].GetString();

        // Act - Use package in the same context without loading again
        var result2 = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            @"JsonConvert.SerializeObject(new { Test = 1 })",
            contextId: contextId
        );

        var json2 = JsonSerializer.Serialize(result2);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json2);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Contains("Test", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task ValidateCsharp_WithoutContext_ValidatesCode()
    {
        // Arrange
        var code = "int x = 10; x * 2";

        // Act
        var result = await ReplTools.ValidateCsharp(_scriptingService, _contextManager, code);

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["isValid"].GetBoolean());
    }
}
