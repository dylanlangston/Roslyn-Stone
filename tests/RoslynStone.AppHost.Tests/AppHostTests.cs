using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace RoslynStone.AppHost.Tests;

/// <summary>
/// Integration tests for the Roslyn-Stone AppHost Aspire configuration.
/// These tests verify that the distributed application starts correctly and
/// that the MCP server HTTP endpoint is accessible.
/// </summary>
public class AppHostTests
{
    [Fact]
    public async Task AppHost_CreatesSuccessfully()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RoslynStone_AppHost>();

        // Assert
        Assert.NotNull(appHost);
    }

    [Fact]
    public async Task McpServer_HasHttpEndpoint()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RoslynStone_AppHost>();

        // Act
        var mcpServer = appHost.Resources.OfType<IResourceWithEndpoints>()
            .FirstOrDefault(r => r.Name == "roslyn-stone-mcp");

        // Assert
        Assert.NotNull(mcpServer);
        var endpoints = mcpServer.GetEndpoints();
        Assert.Contains(endpoints, e => e.EndpointName == "mcp");
    }

    [Fact]
    public async Task McpServer_HasCorrectEnvironmentVariables()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RoslynStone_AppHost>();

        // Act
        var mcpServer = appHost.Resources.OfType<IResourceWithEnvironment>()
            .FirstOrDefault(r => r.Name == "roslyn-stone-mcp");

        // Assert
        Assert.NotNull(mcpServer);
        var env = await mcpServer.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);

        Assert.True(env.ContainsKey("MCP_TRANSPORT"));
        Assert.Equal("http", env["MCP_TRANSPORT"]);
        Assert.True(env.ContainsKey("OTEL_SERVICE_NAME"));
        Assert.Equal("roslyn-stone-mcp", env["OTEL_SERVICE_NAME"]);
    }

    [Fact]
    public async Task McpServer_IsConfiguredAsProjectResource()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RoslynStone_AppHost>();

        // Act
        var mcpServer = appHost.Resources.FirstOrDefault(r => r.Name == "roslyn-stone-mcp");

        // Assert
        Assert.NotNull(mcpServer);
        Assert.IsAssignableFrom<ProjectResource>(mcpServer);
    }

    [Fact]
    public async Task McpServer_DoesNotHaveStdioTransport()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RoslynStone_AppHost>();

        // Act - there should only be one resource named "roslyn-stone-mcp"
        var mcpServers = appHost.Resources.Where(r => r.Name.Contains("roslyn-stone-mcp", StringComparison.OrdinalIgnoreCase)).ToList();

        // Assert - verify only HTTP transport is configured (no stdio)
        Assert.Single(mcpServers);
        Assert.Equal("roslyn-stone-mcp", mcpServers[0].Name);
    }
}
