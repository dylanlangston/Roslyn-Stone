using System.Text.Json;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;
using RoslynStone.Tests.Serialization;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for MCP Tools that provide equivalent functionality to Resources
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "Tools")]
public class ResourceEquivalentToolsTests
{
    private readonly RoslynScriptingService _scriptingService;
    private readonly DocumentationService _documentationService;
    private readonly NuGetService _nugetService;
    private readonly IReplContextManager _contextManager;

    public ResourceEquivalentToolsTests()
    {
        _scriptingService = new RoslynScriptingService();
        _documentationService = new DocumentationService();
        _nugetService = new NuGetService();
        _contextManager = new ReplContextManager();
    }

    #region Documentation Tools Tests

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task GetDocumentation_ValidSymbol_ReturnsDocumentation()
    {
        // Arrange
        var symbolName = "System.String";

        // Act
        var result = await DocumentationTools.GetDocumentation(_documentationService, symbolName);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task GetDocumentation_InvalidSymbol_ReturnsNotFound()
    {
        // Arrange
        var symbolName = "NonExistent.Type.Name";

        // Act
        var result = await DocumentationTools.GetDocumentation(_documentationService, symbolName);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("found", out var foundElement));
        Assert.False(foundElement.GetBoolean());
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task GetDocumentation_WithPackageId_ParsesCorrectly()
    {
        // Arrange
        var symbolName = "Newtonsoft.Json.JsonConvert";
        var packageId = "Newtonsoft.Json";

        // Act
        var result = await DocumentationTools.GetDocumentation(
            _documentationService,
            symbolName,
            packageId
        );

        // Assert
        Assert.NotNull(result);
        // Package may or may not be available, but should not throw
    }

    #endregion

    #region NuGet Tools Tests

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task SearchNuGetPackages_ValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "Newtonsoft.Json";

        // Act
        var result = await NuGetTools.SearchNuGetPackages(_nugetService, query, 0, 5);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("packages", out _));
        Assert.True(dict.TryGetValue("totalCount", out var totalCountElement));
        Assert.True(totalCountElement.GetInt32() > 0);
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task SearchNuGetPackages_WithDefaultPagination_UsesDefaults()
    {
        // Arrange
        var query = "json";

        // Act
        var result = await NuGetTools.SearchNuGetPackages(_nugetService, query);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("skip", out var skipElement));
        Assert.Equal(0, skipElement.GetInt32());
        Assert.True(dict.TryGetValue("take", out var takeElement));
        Assert.Equal(20, takeElement.GetInt32());
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task SearchNuGetPackages_TakeExceedsMax_ClampsTo100()
    {
        // Arrange
        var query = "json";

        // Act
        var result = await NuGetTools.SearchNuGetPackages(_nugetService, query, 0, 200);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("take", out var takeElement));
        Assert.Equal(100, takeElement.GetInt32());
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task GetNuGetPackageVersions_ValidPackage_ReturnsVersions()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";

        // Act
        var result = await NuGetTools.GetNuGetPackageVersions(_nugetService, packageId);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("found", out var foundElement));
        Assert.True(foundElement.GetBoolean());
        Assert.True(dict.TryGetValue("packageId", out var packageIdElement));
        Assert.Equal(packageId, packageIdElement.GetString());
        Assert.True(dict.TryGetValue("versions", out var versionsElement));
        Assert.True(versionsElement.GetArrayLength() > 0);
        Assert.True(dict.TryGetValue("totalCount", out var totalCountElement));
        Assert.True(totalCountElement.GetInt32() > 0);
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task GetNuGetPackageVersions_InvalidPackage_ReturnsNotFound()
    {
        // Arrange
        var packageId = "NonExistentPackage12345XYZ";

        // Act
        var result = await NuGetTools.GetNuGetPackageVersions(_nugetService, packageId);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        // The service returns an empty list for non-existent packages
        // Our tool should still return found=true but with 0 versions
        Assert.True(dict.TryGetValue("found", out var foundElement));
        Assert.True(foundElement.GetBoolean());
        Assert.True(dict.TryGetValue("totalCount", out var totalCountElement));
        Assert.Equal(0, totalCountElement.GetInt32());
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task GetNuGetPackageReadme_ValidPackage_ReturnsReadme()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";

        // Act
        var result = await NuGetTools.GetNuGetPackageReadme(_nugetService, packageId);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("packageId", out var packageIdElement));
        Assert.Equal(packageId, packageIdElement.GetString());
        Assert.True(dict.TryGetValue("content", out _));
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task GetNuGetPackageReadme_WithVersion_ReturnsVersionedReadme()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";
        var version = "13.0.3";

        // Act
        var result = await NuGetTools.GetNuGetPackageReadme(_nugetService, packageId, version);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("packageId", out var packageIdElement));
        Assert.Equal(packageId, packageIdElement.GetString());
        Assert.True(dict.TryGetValue("version", out var versionElement));
        Assert.Equal(version, versionElement.GetString());
    }

    #endregion

    #region REPL Tools Tests

    [Fact]
    [Trait("Feature", "REPL")]
    public void GetReplInfo_WithoutContext_ReturnsGeneralInfo()
    {
        // Act
        var result = ReplTools.GetReplInfo(_scriptingService, _contextManager);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("frameworkVersion", out _));
        Assert.True(dict.TryGetValue("language", out _));
        Assert.True(dict.TryGetValue("capabilities", out _));
        Assert.True(dict.TryGetValue("defaultImports", out _));
        Assert.True(dict.TryGetValue("isSessionSpecific", out var isSessionSpecificElement));
        Assert.False(isSessionSpecificElement.GetBoolean());
    }

    [Fact]
    [Trait("Feature", "REPL")]
    public void GetReplInfo_WithValidContext_ReturnsSessionInfo()
    {
        // Arrange
        var contextId = _contextManager.CreateContext();

        // Act
        var result = ReplTools.GetReplInfo(_scriptingService, _contextManager, contextId);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("contextId", out var contextIdElement));
        Assert.Equal(contextId, contextIdElement.GetString());
        Assert.True(dict.TryGetValue("isSessionSpecific", out var isSessionSpecificElement));
        Assert.True(isSessionSpecificElement.GetBoolean());
        Assert.True(dict.TryGetValue("sessionMetadata", out _));

        // Cleanup
        _contextManager.RemoveContext(contextId);
    }

    [Fact]
    [Trait("Feature", "REPL")]
    public void GetReplInfo_WithInvalidContext_ReturnsGeneralInfo()
    {
        // Arrange
        var contextId = "non-existent-context";

        // Act
        var result = ReplTools.GetReplInfo(_scriptingService, _contextManager, contextId);

        // Assert
        Assert.NotNull(result);
        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);
        Assert.NotNull(dict);
        Assert.True(dict.TryGetValue("contextId", out var contextIdElement));
        Assert.Equal(contextId, contextIdElement.GetString());
        Assert.True(dict.TryGetValue("isSessionSpecific", out var isSessionSpecificElement));
        Assert.True(isSessionSpecificElement.GetBoolean());
        // sessionMetadata should be null for invalid context
        Assert.True(dict.TryGetValue("sessionMetadata", out var sessionMetadataElement));
        Assert.Equal(JsonValueKind.Null, sessionMetadataElement.ValueKind);
    }

    #endregion
}
