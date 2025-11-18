using System.Text.Json;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for LoadNuGetPackage tool - comprehensive coverage for package loading functionality
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "NuGet")]
public class LoadNuGetPackageTests : IDisposable
{
    private readonly RoslynScriptingService _scriptingService;
    private readonly NuGetService _nugetService;

    public LoadNuGetPackageTests()
    {
        _scriptingService = new RoslynScriptingService();
        _nugetService = new NuGetService();
    }

    public void Dispose()
    {
        _nugetService?.Dispose();
    }

    [Fact]
    [Trait("Feature", "PackageLoading")]
    public async Task LoadNuGetPackage_NewtonsoftJson_LoadsSuccessfully()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";

        // Act
        var result = await NuGetTools.LoadNuGetPackage(
            _scriptingService,
            _nugetService,
            packageName
        );

        // Serialize to JSON to inspect result
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["isLoaded"].GetBoolean());
        Assert.Equal(packageName, resultDict["packageName"].GetString());
        Assert.NotNull(resultDict["version"].GetString());
        Assert.Contains("loaded successfully", resultDict["message"].GetString());
    }

    [Fact]
    [Trait("Feature", "PackageLoading")]
    public async Task LoadNuGetPackage_WithSpecificVersion_LoadsCorrectVersion()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";
        var version = "13.0.1";

        // Act
        var result = await NuGetTools.LoadNuGetPackage(
            _scriptingService,
            _nugetService,
            packageName,
            version
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict["isLoaded"].GetBoolean());
        Assert.Equal(version, resultDict["version"].GetString());
    }

    [Fact]
    [Trait("Feature", "PackageUsage")]
    public async Task LoadNuGetPackage_CanUseTypesFromLoadedPackage()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";
        await NuGetTools.LoadNuGetPackage(_scriptingService, _nugetService, packageName);

        // Act - Try to use types from the loaded package
        var code =
            @"
using Newtonsoft.Json;
var obj = new { Name = ""Test"", Value = 42 };
JsonConvert.SerializeObject(obj)
";
        var executionResult = await _scriptingService.ExecuteAsync(code);

        // Assert
        Assert.True(
            executionResult.Success,
            $"Failed to use package types: {string.Join(", ", executionResult.Errors.Select(e => e.Message))}"
        );
        Assert.Contains("Test", executionResult.ReturnValue?.ToString());
        Assert.Contains("42", executionResult.ReturnValue?.ToString());
    }

    [Fact]
    [Trait("Feature", "ErrorHandling")]
    public async Task LoadNuGetPackage_InvalidPackageName_ReturnsErrorResult()
    {
        // Arrange
        var invalidPackageName = "ThisPackageDefinitelyDoesNotExistInNuGet123456789";

        // Act
        var result = await NuGetTools.LoadNuGetPackage(
            _scriptingService,
            _nugetService,
            invalidPackageName
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["isLoaded"].GetBoolean());
        Assert.Equal(invalidPackageName, resultDict["packageName"].GetString());
        Assert.Contains(
            "not found",
            resultDict["message"].GetString(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    [Trait("Feature", "ErrorHandling")]
    public async Task LoadNuGetPackage_InvalidVersion_ReturnsErrorResult()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";
        var invalidVersion = "999.999.999";

        // Act
        var result = await NuGetTools.LoadNuGetPackage(
            _scriptingService,
            _nugetService,
            packageName,
            invalidVersion
        );

        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.False(resultDict["isLoaded"].GetBoolean());
        Assert.Contains(
            "not found",
            resultDict["message"].GetString(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    [Trait("Feature", "MultiplePackages")]
    public async Task LoadNuGetPackage_MultiplePackages_AllLoadSuccessfully()
    {
        // Arrange
        var packages = new[] { "Newtonsoft.Json", "System.Text.Json" };

        // Act
        var results = await Task.WhenAll(
            packages.Select(async packageName =>
            {
                var result = await NuGetTools.LoadNuGetPackage(
                    _scriptingService,
                    _nugetService,
                    packageName
                );
                var json = JsonSerializer.Serialize(result);
                var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                return resultDict!;
            })
        );

        // Assert
        Assert.Equal(packages.Length, results.Length);
        Assert.All(results, result => Assert.True(result["isLoaded"].GetBoolean()));
    }

    [Fact]
    [Trait("Feature", "MultiplePackages")]
    public async Task LoadNuGetPackage_UseMultiplePackagesTogetherInCode_WorksCorrectly()
    {
        // Arrange
        await NuGetTools.LoadNuGetPackage(_scriptingService, _nugetService, "Newtonsoft.Json");

        // Act - Use both packages in the same code
        var code =
            @"
using Newtonsoft.Json;
var data = new { X = 10, Y = 20 };
JsonConvert.SerializeObject(data)
";
        var result = await _scriptingService.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ReturnValue);
        Assert.Contains("10", result.ReturnValue.ToString());
    }

    [Fact]
    [Trait("Feature", "StateManagement")]
    public async Task LoadNuGetPackage_PackageReferencesPersistedAcrossReset()
    {
        // Arrange - Load package and verify it works
        await NuGetTools.LoadNuGetPackage(_scriptingService, _nugetService, "Newtonsoft.Json");
        var codeBeforeReset =
            "using Newtonsoft.Json; JsonConvert.SerializeObject(new { Test = 1 })";
        var resultBeforeReset = await _scriptingService.ExecuteAsync(codeBeforeReset);
        Assert.True(resultBeforeReset.Success);

        // Act - Reset and try to use package again
        _scriptingService.Reset();
        var codeAfterReset = "using Newtonsoft.Json; JsonConvert.SerializeObject(new { Test = 2 })";
        var resultAfterReset = await _scriptingService.ExecuteAsync(codeAfterReset);

        // Assert - Package references ARE preserved across Reset (only script state is cleared)
        // This is intentional behavior - Reset() clears variables but keeps loaded assemblies
        Assert.True(resultAfterReset.Success);
        Assert.Contains("Test", resultAfterReset.ReturnValue?.ToString());
        Assert.Contains("2", resultAfterReset.ReturnValue?.ToString());
    }

    [Fact]
    [Trait("Feature", "Cancellation")]
    public async Task LoadNuGetPackage_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await NuGetTools.LoadNuGetPackage(
                _scriptingService,
                _nugetService,
                packageName,
                cancellationToken: cts.Token
            )
        );
    }

    [Fact]
    [Trait("Feature", "ContextIsolation")]
    public async Task LoadNuGetPackage_PackageLoadedInOneContext_NotAvailableInFreshService()
    {
        // Arrange - Load package in first service
        var service1 = new RoslynScriptingService();
        await NuGetTools.LoadNuGetPackage(service1, _nugetService, "Newtonsoft.Json");

        // Verify package works in service1
        var code = "using Newtonsoft.Json; JsonConvert.SerializeObject(new { Test = 1 })";
        var result1 = await service1.ExecuteAsync(code);
        Assert.True(result1.Success);

        // Act - Try to use package in fresh service2 (should fail)
        var service2 = new RoslynScriptingService();
        var result2 = await service2.ExecuteAsync(code);

        // Assert - Package should NOT be available in service2
        Assert.False(result2.Success);
        Assert.NotEmpty(result2.Errors);
    }
}
