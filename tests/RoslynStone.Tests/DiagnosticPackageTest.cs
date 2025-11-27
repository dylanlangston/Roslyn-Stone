using System.Text.Json;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;
using Xunit.Abstractions;

namespace RoslynStone.Tests;

/// <summary>
/// Diagnostic test to see what's actually happening with package loading
/// </summary>
public class DiagnosticPackageTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly NuGetService _nugetService;
    private readonly IExecutionContextManager _contextManager;

    public DiagnosticPackageTest(ITestOutputHelper output)
    {
        _output = output;
        _nugetService = new NuGetService();
        _contextManager = new ExecutionContextManager();
    }

    public void Dispose()
    {
        _nugetService.Dispose();
    }

    [Fact]
    public async Task Diagnostic_PackageLoading_ShowFullOutput()
    {
        // Arrange
        var nugetPackages = new[]
        {
            new NuGetPackageSpec { PackageName = "Humanizer", Version = "3.0.1" },
        };

        // Act
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            _contextManager,
            _nugetService,
            @"using Humanizer; return ""test"".Humanize();",
            nugetPackages: nugetPackages
        );

        // Serialize and dump everything
        var json = JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions { WriteIndented = true }
        );
        _output.WriteLine("Full result:");
        _output.WriteLine(json);

        // Parse as dictionary
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        if (resultDict == null)
        {
            _output.WriteLine("ERROR: Result is null!");
            return;
        }

        _output.WriteLine("");
        _output.WriteLine($"success: {resultDict["success"]}");

        if (resultDict.ContainsKey("errors"))
        {
            _output.WriteLine($"errors: {resultDict["errors"]}");
        }

        if (resultDict.ContainsKey("warnings"))
        {
            _output.WriteLine($"warnings: {resultDict["warnings"]}");
        }

        if (resultDict.ContainsKey("output"))
        {
            _output.WriteLine($"output: {resultDict["output"]}");
        }
    }
}
