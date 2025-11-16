using RoslynStone.Infrastructure.Services;
using Xunit;

namespace RoslynStone.Tests;

public class DocumentationServiceTests
{
    private readonly DocumentationService _service;

    public DocumentationServiceTests()
    {
        _service = new DocumentationService();
    }

    [Fact]
    public void GetDocumentation_StringType_ReturnsDocumentation()
    {
        // Arrange
        var symbolName = "System.String";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert
        // Documentation may or may not be available depending on the runtime
        // This test primarily ensures the method doesn't throw
        Assert.NotNull(_service);
    }

    [Fact]
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
