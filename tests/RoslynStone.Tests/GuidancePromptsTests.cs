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
        Assert.Contains("Single-File", result);
        Assert.Contains("EvaluateCsharp", result);
        Assert.Contains("ValidateCsharp", result);
        Assert.Contains("doc://", result);
        Assert.Contains("NuGet", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task GetStartedWithCsharpRepl_ContainsPackageDirective()
    {
        // Act
        var result = await GuidancePrompts.GetStartedWithCsharpRepl();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Newtonsoft.Json@13.0.3", result);
        Assert.Contains("dotnet run", result);
        Assert.Contains("no .csproj needed", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task GetStartedWithCsharpRepl_ContainsSdkDirective()
    {
        // Act
        var result = await GuidancePrompts.GetStartedWithCsharpRepl();

        // Assert
        Assert.Contains("#:sdk", result);
        Assert.Contains("Microsoft.NET.Sdk.Web", result);
        Assert.Contains("WebApplication", result);
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
    [Trait("Feature", "NET10Directives")]
    public async Task QuickStartRepl_ContainsPackageDirectiveExample()
    {
        // Act
        var result = await GuidancePrompts.QuickStartRepl();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Humanizer@2.14.1", result);
        Assert.Contains("New .NET 10 Syntax", result);
        Assert.Contains("#:sdk", result);
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
        Assert.Contains("File-Based App", result);
        Assert.Contains("Iterative", result);
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
        Assert.Contains("LoadNuGetPackage", result);
        Assert.Contains("nuget://", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task WorkingWithPackages_ContainsPackageDirective()
    {
        // Act
        var result = await GuidancePrompts.WorkingWithPackages();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Self-contained", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Newtonsoft.Json@13.0.3", result);
        Assert.Contains("CsvHelper@30.0.1", result);
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
        Assert.Contains("LoadNuGetPackage", result);
        Assert.Contains("Newtonsoft.Json", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task PackageIntegrationGuide_ContainsPackageDirectiveInAllExamples()
    {
        // Act
        var result = await GuidancePrompts.PackageIntegrationGuide();

        // Assert - Should contain #:package directive
        Assert.Contains("#:package", result);

        // Assert - All 4 main examples should have package directives
        Assert.Contains("#:package Newtonsoft.Json@13.0.3", result);
        Assert.Contains("#:package Flurl.Http@4.0.0", result);
        Assert.Contains("#:package CsvHelper@30.0.1", result);
        Assert.Contains("#:package Bogus@35.0.0", result);

        // Assert - Should mention self-contained
        Assert.Contains("self-contained", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task ReplBestPractices_ContainsPackageDirectiveGuidance()
    {
        // Act
        var result = await GuidancePrompts.ReplBestPractices();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Package directives", result, StringComparison.OrdinalIgnoreCase);
    }
}
