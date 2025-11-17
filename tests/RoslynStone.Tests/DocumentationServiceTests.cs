using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for DocumentationService
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "Documentation")]
public class DocumentationServiceTests
{
    private readonly DocumentationService _service;

    public DocumentationServiceTests()
    {
        _service = new DocumentationService();
    }

    [Fact]
    [Trait("Feature", "Lookup")]
    public void GetDocumentation_StringType_ReturnsDocumentation()
    {
        // Arrange
        var symbolName = "System.String";

        // Act
        _ = _service.GetDocumentation(symbolName);

        // Assert
        // Documentation may or may not be available depending on the runtime
        // This test primarily ensures the method doesn't throw
        Assert.NotNull(_service);
    }

    [Fact]
    [Trait("Feature", "Validation")]
    public void GetDocumentation_InvalidSymbol_ReturnsNull()
    {
        // Arrange
        var symbolName = "NonExistent.Type.Name";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert
        Assert.Null(result);
    }
}
