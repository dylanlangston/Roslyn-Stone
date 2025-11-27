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
    public async Task QuickStart_ReturnsQuickStartGuide()
    {
        // Act
        var result = await GuidancePrompts.QuickStart();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Quick Start", result);
        Assert.Contains("EvaluateCsharp", result);
        Assert.Contains("nugetPackages", result);
        Assert.Contains("Resources", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task QuickStart_ContainsPackageDirectiveExample()
    {
        // Act
        var result = await GuidancePrompts.QuickStart();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Humanizer@2.14.1", result);
        Assert.Contains("New .NET 10 Syntax", result);
        Assert.Contains("#:sdk", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task ComprehensiveGuide_ReturnsDetailedGuide()
    {
        // Act
        var result = await GuidancePrompts.ComprehensiveGuide();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Complete Guide", result);
        Assert.Contains("Single-File", result);
        Assert.Contains("EvaluateCsharp", result);
        Assert.Contains("ValidateCsharp", result);
        Assert.Contains("doc://", result);
        Assert.Contains("NuGet", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task ComprehensiveGuide_ContainsPackageDirective()
    {
        // Act
        var result = await GuidancePrompts.ComprehensiveGuide();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Newtonsoft.Json@13.0.3", result);
        Assert.Contains("dotnet run", result);
        Assert.Contains("self-contained", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task ComprehensiveGuide_ContainsSdkDirective()
    {
        // Act
        var result = await GuidancePrompts.ComprehensiveGuide();

        // Assert
        Assert.Contains("#:sdk", result);
        Assert.Contains("Microsoft.NET.Sdk.Web", result);
        Assert.Contains("WebApplication", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task ComprehensiveGuide_ContainsPackageDirectiveGuidance()
    {
        // Act
        var result = await GuidancePrompts.ComprehensiveGuide();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Package directives", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task PackageGuide_ReturnsPackageGuide()
    {
        // Act
        var result = await GuidancePrompts.PackageGuide();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("NuGet", result);
        Assert.Contains("nuget://", result);
        Assert.Contains("Newtonsoft.Json", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task PackageGuide_ContainsPackageDirective()
    {
        // Act
        var result = await GuidancePrompts.PackageGuide();

        // Assert
        Assert.Contains("#:package", result);
        Assert.Contains("Self-contained", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Newtonsoft.Json@13.0.3", result);
        Assert.Contains("CsvHelper@30.0.1", result);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    [Trait("Feature", "NET10Directives")]
    public async Task PackageGuide_ContainsPackageDirectiveInAllExamples()
    {
        // Act
        var result = await GuidancePrompts.PackageGuide();

        // Assert - Should contain #:package directive
        Assert.Contains("#:package", result);

        // Assert - All 4 main examples should have package directives
        Assert.Contains("#:package Newtonsoft.Json@13.0.3", result);
        Assert.Contains("#:package Flurl.Http@4.0.0", result);
        Assert.Contains("#:package CsvHelper@30.0.1", result);
        Assert.Contains("#:package Humanizer@2.14.1", result);

        // Assert - Should mention self-contained
        Assert.Contains("self-contained", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Feature", "Prompts")]
    public async Task DebuggingErrors_ReturnsDebugGuide()
    {
        // Act
        var result = await GuidancePrompts.DebuggingErrors();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Debugging", result);
        Assert.Contains("ValidateCsharp", result);
        Assert.Contains("Context-Free", result); // Validation is always context-free (isolated execution)
        Assert.Contains("Workflow", result);
    }
}
