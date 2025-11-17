using RoslynStone.Core.Queries;
using RoslynStone.Infrastructure.QueryHandlers;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for NuGet query handlers
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "QueryHandler")]
public class NuGetQueryHandlerTests
{
    [Fact]
    [Trait("Feature", "Search")]
    public async Task SearchPackagesQueryHandler_ValidQuery_ReturnsResults()
    {
        // Arrange
        var nugetService = new NuGetService();
        var handler = new SearchPackagesQueryHandler(nugetService);
        var query = new SearchPackagesQuery("json", 0, 5);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Packages);
        Assert.Equal("json", result.Query);
    }

    [Fact]
    [Trait("Feature", "Versions")]
    public async Task GetPackageVersionsQueryHandler_ValidPackage_ReturnsVersions()
    {
        // Arrange
        var nugetService = new NuGetService();
        var handler = new GetPackageVersionsQueryHandler(nugetService);
        var query = new GetPackageVersionsQuery("Newtonsoft.Json");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, v => Assert.False(string.IsNullOrEmpty(v.Version)));
    }

    [Fact(Skip = "README extraction can be slow")]
    [Trait("Feature", "README")]
    public async Task GetPackageReadmeQueryHandler_ValidPackage_ReturnsReadme()
    {
        // Arrange
        var nugetService = new NuGetService();
        var handler = new GetPackageReadmeQueryHandler(nugetService);
        var query = new GetPackageReadmeQuery("Newtonsoft.Json");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert - may be null if package doesn't have README
        Assert.True(result == null || !string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    [Trait("Feature", "README")]
    public async Task GetPackageReadmeQueryHandler_WithVersion_ReturnsReadme()
    {
        // Arrange
        var nugetService = new NuGetService();
        var handler = new GetPackageReadmeQueryHandler(nugetService);
        var query = new GetPackageReadmeQuery("Newtonsoft.Json", "13.0.3");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert - may be null if package doesn't have README
        Assert.True(result == null || !string.IsNullOrWhiteSpace(result));
    }
}
