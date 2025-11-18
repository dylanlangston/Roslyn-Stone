using ModelContextProtocol.Server;
using RoslynStone.Infrastructure.Tools;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for MCP server configuration and tool discovery
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "MCP")]
public class McpServerConfigurationTests
{
    [Fact]
    [Trait("Feature", "ToolDiscovery")]
    public void ReplTools_HasExpectedToolCount()
    {
        // This test verifies that we have the expected number of tools
        // to detect if tools are accidentally removed or duplicated

        var methods = typeof(ReplTools)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), false).Any())
            .ToList();

        // We expect: EvaluateCsharp, ValidateCsharp, ResetRepl, GetReplInfo
        Assert.Equal(4, methods.Count);
    }

    [Fact]
    [Trait("Feature", "ToolDiscovery")]
    public void DocumentationTools_HasExpectedToolCount()
    {
        var methods = typeof(DocumentationTools)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), false).Any())
            .ToList();

        // We expect: GetDocumentation
        Assert.Single(methods);
    }

    [Fact]
    [Trait("Feature", "ToolDiscovery")]
    public void NuGetTools_HasExpectedToolCount()
    {
        var methods = typeof(NuGetTools)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), false).Any())
            .ToList();

        // We expect: SearchNuGetPackages, GetNuGetPackageVersions, GetNuGetPackageReadme, LoadNuGetPackage
        Assert.Equal(4, methods.Count);
    }

    [Fact]
    [Trait("Feature", "PromptDiscovery")]
    public void GuidancePrompts_HasExpectedPromptCount()
    {
        var methods = typeof(GuidancePrompts)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(McpServerPromptAttribute), false).Any())
            .ToList();

        // We expect: QuickStartRepl, GetStartedWithCsharpRepl, DebugCompilationErrors,
        //            ReplBestPractices, WorkingWithPackages, PackageIntegrationGuide
        Assert.Equal(6, methods.Count);
    }

    [Fact]
    [Trait("Feature", "ToolDiscovery")]
    public void AllTools_HaveDescriptions()
    {
        // Verify all tools have [Description] attributes for MCP protocol
        var toolTypes = new[] { typeof(ReplTools), typeof(DocumentationTools), typeof(NuGetTools) };

        foreach (var toolType in toolTypes)
        {
            var methods = toolType
                .GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), false).Any())
                .ToList();

            foreach (var method in methods)
            {
                var descriptionAttr = method
                    .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                    .FirstOrDefault();

                Assert.NotNull(descriptionAttr);

                var description = (
                    (System.ComponentModel.DescriptionAttribute)descriptionAttr
                ).Description;
                Assert.NotEmpty(description);

                // Verify description is substantial (not just a few words)
                Assert.True(
                    description.Length > 50,
                    $"Tool {toolType.Name}.{method.Name} has a description that is too short: {description}"
                );
            }
        }
    }

    [Fact]
    [Trait("Feature", "PromptDiscovery")]
    public void AllPrompts_HaveDescriptions()
    {
        // Verify all prompts have [Description] attributes for MCP protocol
        var methods = typeof(GuidancePrompts)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(McpServerPromptAttribute), false).Any())
            .ToList();

        foreach (var method in methods)
        {
            var descriptionAttr = method
                .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .FirstOrDefault();

            Assert.NotNull(descriptionAttr);

            var description = (
                (System.ComponentModel.DescriptionAttribute)descriptionAttr
            ).Description;
            Assert.NotEmpty(description);

            // Verify description is substantial
            Assert.True(
                description.Length > 30,
                $"Prompt {method.Name} has a description that is too short: {description}"
            );
        }
    }
}
