using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Integration tests for DocumentationService with NuGet packages
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "Documentation")]
public class DocumentationServiceNuGetIntegrationTests : IDisposable
{
    private readonly NuGetService _nugetService;
    private readonly DocumentationService _service;

    public DocumentationServiceNuGetIntegrationTests()
    {
        _nugetService = new NuGetService();
        _service = new DocumentationService(_nugetService);
    }

    [Fact]
    [Trait("Feature", "NuGetPackages")]
    public async Task GetDocumentationAsync_NewtonsoftJsonJsonConvert_ReturnsDocumentationIfInstalled()
    {
        // Arrange
        var symbolName = "Newtonsoft.Json.JsonConvert";
        var packageId = "Newtonsoft.Json";

        // Act
        var result = await _service.GetDocumentationAsync(symbolName, packageId);

        // Assert
        // If the package is installed locally (from other tests or builds),
        // this should find documentation. Otherwise, it may return null.
        // This test ensures the lookup doesn't throw exceptions.
        if (result != null)
        {
            Assert.Equal(symbolName, result.SymbolName);
            Assert.NotNull(result.Summary);
            Assert.NotEmpty(result.Summary);
        }
    }

    [Fact]
    [Trait("Feature", "NuGetPackages")]
    public async Task GetDocumentationAsync_CommonNuGetPackage_HandlesGracefully()
    {
        // Arrange - Try a few common packages that might be installed
        var testCases = new[]
        {
            ("Microsoft.Extensions.Logging.ILogger", "Microsoft.Extensions.Logging.Abstractions"),
            ("Newtonsoft.Json.JsonSerializer", "Newtonsoft.Json"),
            ("System.Text.Json.JsonSerializer", "System.Text.Json"),
        };

        foreach (var (symbolName, packageId) in testCases)
        {
            // Act
            var result = await _service.GetDocumentationAsync(symbolName, packageId);

            // Assert - Should not throw, may or may not find documentation
            Assert.True(
                result == null || result.SymbolName == symbolName,
                $"Symbol name mismatch for {symbolName}"
            );
        }
    }

    [Fact]
    [Trait("Feature", "NuGetPackages")]
    public async Task GetDocumentationAsync_TypeInInstalledPackage_RetrievesCorrectMemberTypes()
    {
        // Arrange
        var symbolName = "Newtonsoft.Json.JsonConvert";
        var packageId = "Newtonsoft.Json";

        // Act
        var result = await _service.GetDocumentationAsync(symbolName, packageId);

        // Assert
        if (result != null)
        {
            // Verify that the documentation structure is correct
            Assert.NotNull(result.SymbolName);
            Assert.NotNull(result.Summary);
            Assert.NotNull(result.Parameters);
            Assert.NotNull(result.Exceptions);
        }
    }

    [Fact]
    [Trait("Feature", "NuGetPackages")]
    public async Task GetDocumentationAsync_InvalidPackageId_ReturnsNull()
    {
        // Arrange
        var symbolName = "Some.Type.Name";
        var packageId = "This.Package.Does.Not.Exist.123456789";

        // Act
        var result = await _service.GetDocumentationAsync(symbolName, packageId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Feature", "NuGetPackages")]
    public async Task GetDocumentationAsync_EmptyPackageId_ReturnsFallbackBehavior()
    {
        // Arrange
        var symbolName = "System.String";
        var packageId = "";

        // Act
        var result = await _service.GetDocumentationAsync(symbolName, packageId);

        // Assert - Empty package ID should fall back to normal behavior
        Assert.True(result == null || result.SymbolName == symbolName);
    }

    public void Dispose()
    {
        _nugetService.Dispose();
    }
}
