using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RoslynStone.Core.Commands;
using RoslynStone.Infrastructure.CommandHandlers;
using RoslynStone.Infrastructure.Services;
using Xunit;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for LoadPackageCommandHandler
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "CommandHandler")]
public class LoadPackageCommandHandlerTests
{
    [Fact(Skip = "Package download can be slow")]
    [Trait("Feature", "Loading")]
    public async Task HandleAsync_ValidPackage_LoadsSuccessfully()
    {
        // Arrange
        var scriptingService = new RoslynScriptingService();
        var nugetService = new NuGetService();
        var logger = NullLogger<LoadPackageCommandHandler>.Instance;
        var handler = new LoadPackageCommandHandler(scriptingService, nugetService, logger);
        var command = new LoadPackageCommand("Newtonsoft.Json", "13.0.3");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Newtonsoft.Json", result.Name);
        Assert.Equal("13.0.3", result.Version);
        Assert.True(result.IsLoaded);
    }

    [Fact]
    [Trait("Feature", "Loading")]
    public async Task HandleAsync_InvalidPackage_ReturnsNotLoaded()
    {
        // Arrange
        var scriptingService = new RoslynScriptingService();
        var nugetService = new NuGetService();
        var logger = NullLogger<LoadPackageCommandHandler>.Instance;
        var handler = new LoadPackageCommandHandler(scriptingService, nugetService, logger);
        var command = new LoadPackageCommand("ThisPackageDefinitelyDoesNotExist12345XYZ");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ThisPackageDefinitelyDoesNotExist12345XYZ", result.Name);
        Assert.False(result.IsLoaded);
    }
}
