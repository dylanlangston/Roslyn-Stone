using System.Text.Json;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;
using RoslynStone.Tests.Serialization;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for REPL tools context control parameter functionality
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "REPL")]
public class ReplToolsContextControlTests : IDisposable
{
    private readonly IExecutionContextManager _contextManager;
    private readonly NuGetService _nugetService;

    public ReplToolsContextControlTests()
    {
        _contextManager = new ExecutionContextManager();
        _nugetService = new NuGetService();
    }

    public void Dispose()
    {
        _nugetService.Dispose();
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task EvaluateCsharp_CreateContextFalse_DoesNotReturnContextId()
    {
        // Arrange
        var code = "1 + 1";

        // Act
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code,
            createContext: false
        );

        // Serialize to inspect result
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal("2", resultDict["returnValue"].GetString());
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
        Assert.False(
            resultDict.TryGetValue("contextId", out var contextIdElem)
                && contextIdElem.ValueKind != JsonValueKind.Null
        );
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
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code,
            nugetPackages: packages,
            createContext: false
        );

        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

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
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code,
            nugetPackages: packages,
            createContext: false
        );

        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

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
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code,
            nugetPackages: null,
            createContext: false
        );

        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal("2", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "NuGetIntegration")]
    public async Task EvaluateCsharp_WithEmptyNuGetPackages_ExecutesWithoutPackageLoading()
    {
        // Arrange
        var code = "1 + 1";

        // Act
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code,
            nugetPackages: Array.Empty<NuGetPackageSpec>(),
            createContext: false
        );

        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal("2", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task ValidateCsharp_CreateContextFalse_ReturnsValidationWithoutContextId()
    {
        // Arrange & Act - Execute code in temporary context
        await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            "var x = 10;",
            createContext: false
        );

        // Try to access variable from previous execution
        var result2 = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
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
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            code,
            nugetPackages: packages,
            createContext: false
        );

        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Contains("10", resultDict["returnValue"].GetString());
    }

    [Fact]
    [Trait("Feature", "ContextControl")]
    public async Task ValidateCsharp_WithoutContext_ValidatesCode()
    {
        // Arrange
        var code = "int x = 10; return x * 2;";

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
}
