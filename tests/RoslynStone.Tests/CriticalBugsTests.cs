using System.Text.Json;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for critical bugs discovered in MCP audit
/// These tests should FAIL initially, then PASS after fixes are applied (TDD style)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "CriticalBugs")]
public class CriticalBugsTests : IDisposable
{
    private readonly RoslynScriptingService _scriptingService;
    private readonly NuGetService _nugetService;
    private readonly DocumentationService _documentationService;
    private readonly IReplContextManager _contextManager;

    public CriticalBugsTests()
    {
        _scriptingService = new RoslynScriptingService();
        _nugetService = new NuGetService();
        _documentationService = new DocumentationService(_nugetService);
        _contextManager = new ReplContextManager();
    }

    public void Dispose()
    {
        _nugetService?.Dispose();
        if (_contextManager is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Critical Issue #1: LoadNuGetPackage Context Isolation

    [Fact]
    [Trait("Bug", "PackageContextIsolation")]
    public async Task BUG_LoadNuGetPackage_ThenEvaluateCsharp_PackageShouldBeAvailable()
    {
        // This test documents the EXPECTED behavior
        // Currently FAILS due to package context isolation bug

        // Arrange - Load package via tool
        await NuGetTools.LoadNuGetPackage(_scriptingService, _nugetService, "Humanizer");

        // Act - Try to use the package in EvaluateCsharp (same scripting service)
        var code = @"using Humanizer; return ""PascalCase"".Humanize();";
        var result = await _scriptingService.ExecuteAsync(code);

        // Assert - Package should be available
        Assert.True(
            result.Success,
            $"Package should be available after LoadNuGetPackage. Errors: {string.Join(", ", result.Errors.Select(e => e.Message))}"
        );
        Assert.Equal("Pascal case", result.ReturnValue?.ToString());
    }

    [Fact]
    [Trait("Bug", "PackageContextIsolation")]
    public async Task BUG_LoadNuGetPackage_ViaReplTools_ShouldPersistForSubsequentCalls()
    {
        // This tests the ReplTools.EvaluateCsharp with LoadNuGetPackage integration
        // Currently FAILS

        // Arrange - Load package using ReplTools
        await NuGetTools.LoadNuGetPackage(_scriptingService, _nugetService, "Humanizer");

        // Act - Call EvaluateCsharp via ReplTools (simulates MCP client call)
        var code = @"using Humanizer; return ""test"".Humanize();";
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            code
        );

        // Assert - Should succeed
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        Assert.NotNull(resultDict);
        Assert.True(
            resultDict["success"].GetBoolean(),
            "Package loaded via LoadNuGetPackage should be available in EvaluateCsharp"
        );
    }

    [Fact]
    [Trait("Bug", "PackageContextIsolation")]
    public async Task BUG_LoadNuGetPackage_WithContextId_PackageShouldPersist()
    {
        // Test stateful sessions with packages
        // Currently FAILS

        // Arrange - Create context and load package
        var createResult = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "var x = 10;",
            createContext: true
        );

        var json = JsonSerializer.Serialize(createResult);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        var contextId = dict!["contextId"].GetString()!;

        await NuGetTools.LoadNuGetPackage(_scriptingService, _nugetService, "Humanizer");

        // Act - Use package in same context
        var useResult = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            @"using Humanizer; return ""Test"".Humanize();",
            contextId: contextId
        );

        // Assert
        var useJson = JsonSerializer.Serialize(useResult);
        var useDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(useJson);

        Assert.NotNull(useDict);
        Assert.True(useDict["success"].GetBoolean(), "Package should be available in context");
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
            "System.Linq.Enumerable"
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
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
        var createResult = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "var x = 10;",
            createContext: true
        );

        var json = JsonSerializer.Serialize(createResult);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        var contextId = dict!["contextId"].GetString()!;

        // Act - Continue with context
        var continueResult = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "return x * 2;",
            contextId: contextId
        );

        // Assert - Should succeed or provide detailed error
        var continueJson = JsonSerializer.Serialize(continueResult);
        var continueDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(continueJson);

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
        // Test error message quality for invalid context
        // Currently may return generic error

        // Act - Use non-existent context
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            "return 42;",
            contextId: "non-existent-context-id"
        );

        // Assert - Should provide helpful error
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.NotEmpty(errors);

        var errorMessage = errors[0].GetProperty("message").GetString()!;
        Assert.Contains("context", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("An error occurred", errorMessage);
    }

    #endregion

    #region High Priority: nugetPackages Parameter Testing

    [Fact]
    [Trait("Feature", "NuGetPackagesParameter")]
    public async Task EvaluateCsharp_WithNugetPackagesParameter_ShouldLoadAndUse()
    {
        // Test the documented way to load packages inline
        // This should work if package context is fixed

        // Arrange
        var nugetPackages = new[]
        {
            new NuGetPackageSpec { PackageName = "Humanizer", Version = "3.0.1" }
        };

        // Act
        var result = await ReplTools.EvaluateCsharp(
            _scriptingService,
            _contextManager,
            _nugetService,
            @"using Humanizer; return ""Test"".Humanize();",
            nugetPackages: nugetPackages
        );

        // Assert
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        Assert.NotNull(resultDict);
        Assert.True(
            resultDict["success"].GetBoolean(),
            "Package specified via nugetPackages parameter should be available"
        );
        Assert.Equal("Test", resultDict["returnValue"].GetString());
    }

    #endregion
}
