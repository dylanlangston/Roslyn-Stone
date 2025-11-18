using System.Text.Json;
using RoslynStone.Infrastructure.Resources;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for MCP Resources
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "Resources")]
public class ResourceTests
{
    private readonly RoslynScriptingService _scriptingService;
    private readonly DocumentationService _documentationService;
    private readonly NuGetService _nugetService;
    private readonly IReplContextManager _contextManager;

    public ResourceTests()
    {
        _scriptingService = new RoslynScriptingService();
        _documentationService = new DocumentationService();
        _nugetService = new NuGetService();
        _contextManager = new ReplContextManager();
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task DocumentationResource_GetDocumentation_ValidUri_ReturnsDocumentation()
    {
        // Arrange
        var uri = "doc://System.String";

        // Act
        var result = await DocumentationResource.GetDocumentation(_documentationService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        Assert.True(resultDict.TryGetValue("mimeType", out var mimeTypeElement));
        Assert.Equal("application/json", mimeTypeElement.GetString());
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task DocumentationResource_GetDocumentation_WithoutPrefix_ReturnsDocumentation()
    {
        // Arrange
        var uri = "System.String";

        // Act
        var result = await DocumentationResource.GetDocumentation(_documentationService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out _));
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task DocumentationResource_GetDocumentation_InvalidSymbol_ReturnsNotFound()
    {
        // Arrange
        var uri = "doc://NonExistent.Type.Name";

        // Act
        var result = await DocumentationResource.GetDocumentation(_documentationService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("found", out var foundElement));
        Assert.False(foundElement.GetBoolean());
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetSearchResource_SearchPackages_ValidQuery_ReturnsResults()
    {
        // Arrange
        var uri = "nuget://search?q=Newtonsoft.Json&skip=0&take=5";

        // Act
        var result = await NuGetSearchResource.SearchPackages(_nugetService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        Assert.True(resultDict.TryGetValue("packages", out _));
        Assert.True(resultDict.TryGetValue("totalCount", out var totalCountElement));
        Assert.True(totalCountElement.GetInt32() > 0);
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetSearchResource_SearchPackages_WithoutQueryParams_UsesDefaults()
    {
        // Arrange
        var uri = "nuget://search?q=json";

        // Act
        var result = await NuGetSearchResource.SearchPackages(_nugetService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("skip", out var skipElem));
        Assert.Equal(0, skipElem.GetInt32());
        Assert.True(resultDict.TryGetValue("take", out var takeElem));
        Assert.Equal(20, takeElem.GetInt32());
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetPackageResource_GetPackageVersions_ValidPackage_ReturnsVersions()
    {
        // Arrange
        var uri = "nuget://packages/Newtonsoft.Json/versions";

        // Act
        var result = await NuGetPackageResource.GetPackageVersions(_nugetService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("found", out var foundElement));
        Assert.True(foundElement.GetBoolean());
        Assert.True(resultDict.TryGetValue("packageId", out var packageIdElement));
        Assert.Equal("Newtonsoft.Json", packageIdElement.GetString());
        Assert.True(resultDict.TryGetValue("versions", out var versionsElement));
        Assert.True(versionsElement.GetArrayLength() > 0);
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetPackageResource_GetPackageVersions_InvalidUri_ReturnsNotFound()
    {
        // Arrange
        var uri = "nuget://packages//versions";

        // Act
        var result = await NuGetPackageResource.GetPackageVersions(_nugetService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("found", out var foundElement));
        Assert.False(foundElement.GetBoolean());
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetPackageResource_GetPackageReadme_ValidPackage_ReturnsReadme()
    {
        // Arrange
        var uri = "nuget://packages/Newtonsoft.Json/readme";

        // Act
        var result = await NuGetPackageResource.GetPackageReadme(_nugetService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.True(resultDict.TryGetValue("mimeType", out var mimeTypeElement));
        Assert.Equal("text/markdown", mimeTypeElement.GetString());
        // README structure should be present (content key might be present)
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetPackageResource_GetPackageReadme_WithVersion_ReturnsVersionedReadme()
    {
        // Arrange
        var uri = "nuget://packages/Newtonsoft.Json/readme?version=13.0.3";

        // Act
        var result = await NuGetPackageResource.GetPackageReadme(_nugetService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out _));
        Assert.True(resultDict.TryGetValue("mimeType", out _));
        // Version should be present in the response
    }

    [Fact]
    [Trait("Feature", "REPL")]
    public void ReplStateResource_GetReplState_ReturnsState()
    {
        // Arrange
        var uri = "repl://state";

        // Act
        var result = ReplStateResource.GetReplState(_scriptingService, _contextManager, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        Assert.True(resultDict.TryGetValue("frameworkVersion", out _));
        Assert.True(resultDict.TryGetValue("language", out _));
        Assert.True(resultDict.TryGetValue("capabilities", out _));
        Assert.True(resultDict.TryGetValue("defaultImports", out _));
    }

    [Fact]
    [Trait("Feature", "REPL")]
    public void ReplStateResource_GetReplState_AlternativeUri_ReturnsState()
    {
        // Arrange
        var uri = "repl://info";

        // Act
        var result = ReplStateResource.GetReplState(_scriptingService, _contextManager, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
    }

    [Fact]
    [Trait("Feature", "REPL")]
    public void ReplStateResource_GetReplState_SessionSpecificUri_ReturnsSessionState()
    {
        // Arrange
        var uri = "repl://sessions/test-context-123/state";

        // Act
        var result = ReplStateResource.GetReplState(_scriptingService, _contextManager, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        Assert.True(resultDict.TryGetValue("isSessionSpecific", out var isSessionSpecific));
        Assert.True(isSessionSpecific.GetBoolean());
        Assert.True(resultDict.TryGetValue("contextId", out var contextIdElement));
        Assert.Equal("test-context-123", contextIdElement.GetString());
    }
}
