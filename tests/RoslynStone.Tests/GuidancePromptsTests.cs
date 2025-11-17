using RoslynStone.Infrastructure.Tools;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for MCP guidance prompts
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "MCP")]
public class GuidancePromptsTests
{
    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task GetStartedWithCsharpRepl_ReturnsDetailedGuide()
    {
        // Act
        var result = await GuidancePrompts.GetStartedWithCsharpRepl();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Roslyn-Stone", result);
        Assert.Contains("REPL", result);
        Assert.Contains("EvaluateCsharp", result);
        Assert.Contains("ValidateCsharp", result);
        Assert.Contains("GetDocumentation", result);
        Assert.Contains("NuGet", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task CodeExperimentationWorkflow_ReturnsWorkflowGuide()
    {
        // Act
        var result = await GuidancePrompts.CodeExperimentationWorkflow();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Experimentation", result);
        Assert.Contains("Validate", result);
        Assert.Contains("Execute", result);
        Assert.Contains("Iterate", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task PackageIntegrationGuide_ReturnsPackageGuide()
    {
        // Act
        var result = await GuidancePrompts.PackageIntegrationGuide();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("NuGet", result);
        Assert.Contains("SearchNuGetPackages", result);
        Assert.Contains("LoadNuGetPackage", result);
        Assert.Contains("Newtonsoft.Json", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task DebuggingAndErrorHandling_ReturnsDebuggingGuide()
    {
        // Act
        var result = await GuidancePrompts.DebuggingAndErrorHandling();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Debugging", result);
        Assert.Contains("Error", result);
        Assert.Contains("Compilation", result);
        Assert.Contains("Runtime", result);
    }
}
