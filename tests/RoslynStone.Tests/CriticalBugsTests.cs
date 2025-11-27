using System.Text.Json;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;
using RoslynStone.Tests.Serialization;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for critical bugs discovered in MCP audit
/// These tests should FAIL initially, then PASS after fixes are applied (TDD style)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "CriticalBugs")]
public class CriticalBugsTests : IDisposable
{
    private readonly NuGetService _nugetService;
    private readonly DocumentationService _documentationService;
    private readonly IExecutionContextManager _contextManager;

    public CriticalBugsTests()
    {
        _nugetService = new NuGetService();
        _documentationService = new DocumentationService(_nugetService);
        _contextManager = new ExecutionContextManager();
    }

    public void Dispose()
    {
        _nugetService.Dispose();
    }

    #region Package Loading in Context

    [Fact]
    [Trait("Feature", "NuGetPackagesParameter")]
    public async Task EvaluateCsharp_WithNugetPackagesParameter_SingleShot_ShouldWork()
    {
        // Test single-shot execution with packages (temporary isolated context)
        // Context is created, used, and destroyed automatically

        // Arrange
        var nugetPackages = new[]
        {
            new NuGetPackageSpec { PackageName = "Humanizer", Version = "3.0.1" },
        };

        // Act - Single-shot execution (createContext=false, no contextId)
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            @"using Humanizer; return ""test"".Humanize();",
            nugetPackages: nugetPackages
        );

        // Assert - Should succeed
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        Assert.NotNull(resultDict);
        Assert.True(
            resultDict["success"].GetBoolean(),
            "Package should be available in isolated context"
        );
        Assert.Equal("Test", resultDict["returnValue"].GetString());

        // Context should not be returned (it was temporary)
        Assert.False(
            resultDict.TryGetValue("contextId", out var contextIdElement)
                && contextIdElement.ValueKind != JsonValueKind.Null,
            "Temporary context should not return contextId"
        );
    }

    #endregion

    #region Critical Issue #2: GetDocumentation Fails for Core Types

    [Fact]
    [Trait("Bug", "DocumentationLookup")]
    public async Task BUG_GetDocumentation_SystemString_ShouldReturnDocumentation()
    {
        // Currently FAILS - returns null or "not found"
        // Should return documentation for System.String

        // Act
        var result = await _documentationService.GetDocumentationAsync("System.String");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("System.String", result.SymbolName);
        Assert.False(string.IsNullOrEmpty(result.Summary), "Summary should not be empty");
    }

    [Fact]
    [Trait("Bug", "DocumentationLookup")]
    public async Task BUG_GetDocumentation_SystemConsole_ShouldReturnDocumentation()
    {
        // Currently FAILS

        // Act
        var result = await _documentationService.GetDocumentationAsync("System.Console");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("System.Console", result.SymbolName);
        Assert.NotNull(result.Summary);
    }

    [Fact]
    [Trait("Bug", "DocumentationLookup")]
    public async Task BUG_GetDocumentation_CommonTypes_ShouldAllWork()
    {
        // Test common types that are documented in prompts
        // Currently FAILS

        var commonTypes = new[]
        {
            "System.String",
            "System.Int32",
            "System.Console",
            "System.IO.File",
            "System.Linq.Enumerable",
        };

        foreach (var typeName in commonTypes)
        {
            // Act
            var result = await _documentationService.GetDocumentationAsync(typeName);

            // Assert
            Assert.NotNull(result);
            Assert.False(
                string.IsNullOrEmpty(result.Summary),
                $"Documentation for {typeName} should have a summary"
            );
        }
    }

    [Fact]
    [Trait("Bug", "DocumentationLookup")]
    public async Task BUG_DocumentationTools_SystemString_ShouldReturnSuccess()
    {
        // Test the MCP tool directly
        // Currently FAILS

        // Act
        var result = await DocumentationTools.GetDocumentation(
            _documentationService,
            "System.String"
        );

        // Assert - Tool should return success response
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        Assert.NotNull(resultDict);
        Assert.True(
            resultDict["found"].GetBoolean(),
            "GetDocumentation tool should find System.String"
        );
        Assert.False(
            string.IsNullOrEmpty(resultDict["summary"].GetString()),
            "Summary should not be empty"
        );
    }

    #endregion

    #region Critical Issue #3: Context State Errors

    [Fact]
    [Trait("Bug", "ContextState")]
    public async Task BUG_EvaluateCsharp_WithContextId_ShouldNotFailWithGenericError()
    {
        // Test that context operations provide detailed errors
        // Currently FAILS with "An error occurred"

        // Arrange - Create context
        var createResult = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            "var x = 10;",
            createContext: true
        );

        var json = TestJsonContext.SerializeDynamic(createResult);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        var contextId = dict!["contextId"].GetString()!;

        // Act - Continue with context
        var continueResult = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            "return x * 2;",
            contextId: contextId
        );

        // Assert - Should succeed or provide detailed error
        var continueJson = TestJsonContext.SerializeDynamic(continueResult);
        var continueDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            continueJson
        );

        Assert.NotNull(continueDict);

        if (!continueDict["success"].GetBoolean())
        {
            // If it fails, error should be detailed, not generic
            var errors = continueDict["errors"].EnumerateArray().ToList();
            Assert.NotEmpty(errors);
            Assert.All(
                errors,
                error =>
                    Assert.DoesNotContain(
                        "An error occurred",
                        error.GetProperty("message").GetString(),
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }
        else
        {
            // Should succeed and return 20
            Assert.Equal(20, continueDict["returnValue"].GetInt32());
        }
    }

    [Fact]
    [Trait("Bug", "ContextState")]
    public async Task BUG_EvaluateCsharp_InvalidContextId_ShouldProvideHelpfulError()
    {
        // Test that isolated execution doesn't require pre-existing context
        // In isolated mode, context IDs are created on-demand and don't track state

        // Act - Use non-existent context (will be created on-demand in isolated execution)
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            "return 42;",
            contextId: "non-existent-context-id"
        );

        // Assert - Should succeed since isolated execution creates contexts on-demand
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        Assert.NotNull(resultDict);
        // In isolated execution, invalid contextId just creates a new isolated context
        Assert.True(resultDict["success"].GetBoolean());
        Assert.Equal("42", resultDict["returnValue"].GetString());
    }

    #endregion
}
