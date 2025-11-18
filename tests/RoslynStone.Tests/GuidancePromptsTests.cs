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
    public async Task QuickStartRepl_ReturnsQuickStartGuide()
    {
        // Act
        var result = await GuidancePrompts.QuickStartRepl();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Quick Start", result);
        Assert.Contains("EvaluateCsharp", result);
        Assert.Contains("contextId", result);
        Assert.Contains("Resources", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task DebugCompilationErrors_ReturnsDebugGuide()
    {
        // Act
        var result = await GuidancePrompts.DebugCompilationErrors();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Debugging", result);
        Assert.Contains("ValidateCsharp", result);
        Assert.Contains("Context-Aware", result);
        Assert.Contains("Workflow", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task ReplBestPractices_ReturnsBestPracticesGuide()
    {
        // Act
        var result = await GuidancePrompts.ReplBestPractices();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Best Practices", result);
        Assert.Contains("Session Management", result);
        Assert.Contains("Incremental Development", result);
        Assert.Contains("contextId", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task WorkingWithPackages_ReturnsPackageWorkflowGuide()
    {
        // Act
        var result = await GuidancePrompts.WorkingWithPackages();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("NuGet", result);
        Assert.Contains("SearchNuGetPackages", result);
        Assert.Contains("LoadNuGetPackage", result);
        Assert.Contains("nuget://", result);
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
}
