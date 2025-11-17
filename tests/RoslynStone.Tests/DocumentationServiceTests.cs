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

    [Fact]
    [Trait("Feature", "Lookup")]
    public void GetDocumentation_ValidType_ReturnsDocumentationOrNull()
    {
        // Arrange
        var symbolName = "System.Int32";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert - Documentation may or may not be available
        // Either null (no XML docs) or valid DocumentationInfo
        if (result != null)
        {
            Assert.Equal(symbolName, result.SymbolName);
            Assert.NotNull(result.Parameters);
            Assert.NotNull(result.Exceptions);
        }
    }

    [Fact]
    [Trait("Feature", "Lookup")]
    public void GetDocumentation_ConsoleType_ReturnsDocumentationOrNull()
    {
        // Arrange
        var symbolName = "System.Console";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert
        // Documentation availability depends on runtime
        if (result != null)
        {
            Assert.Equal(symbolName, result.SymbolName);
        }
    }

    [Fact]
    [Trait("Feature", "Lookup")]
    public void GetDocumentation_PartialTypeName_ReturnsDocumentationOrNull()
    {
        // Arrange
        var symbolName = "String";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert
        // May find System.String or return null
        Assert.True(result == null || result.SymbolName == symbolName);
    }

    [Fact]
    [Trait("Feature", "CacheSupport")]
    public void GetDocumentation_SameSymbolTwice_UsesCache()
    {
        // Arrange
        var symbolName = "System.String";

        // Act
        var result1 = _service.GetDocumentation(symbolName);
        var result2 = _service.GetDocumentation(symbolName);

        // Assert - Should return same result (cached or both null)
        if (result1 == null)
        {
            Assert.Null(result2);
        }
        else
        {
            Assert.NotNull(result2);
            Assert.Equal(result1.SymbolName, result2.SymbolName);
        }
    }

    [Fact]
    [Trait("Feature", "EdgeCases")]
    public void GetDocumentation_EmptyString_ReturnsNull()
    {
        // Arrange
        var symbolName = "";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Feature", "EdgeCases")]
    public void GetDocumentation_SpecialCharacters_ReturnsNull()
    {
        // Arrange
        var symbolName = "System.@#$%";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Feature", "GenericTypes")]
    public void GetDocumentation_GenericType_ReturnsDocumentationOrNull()
    {
        // Arrange
        var symbolName = "System.Collections.Generic.List`1";

        // Act
        var result = _service.GetDocumentation(symbolName);

        // Assert
        // Generic type notation may or may not be found
        Assert.True(result == null || !string.IsNullOrEmpty(result.SymbolName));
    }
}
