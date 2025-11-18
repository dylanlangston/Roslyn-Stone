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
    public void DocumentationResource_GetDocumentation_ValidUri_ReturnsDocumentation()
    {
        // Arrange
        var uri = "doc://System.String";

        // Act
        var result = DocumentationResource.GetDocumentation(_documentationService, uri);
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
    public void DocumentationResource_GetDocumentation_WithoutPrefix_ReturnsDocumentation()
    {
        // Arrange
        var uri = "System.String";

        // Act
        var result = DocumentationResource.GetDocumentation(_documentationService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.ContainsKey("uri"));
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public void DocumentationResource_GetDocumentation_InvalidSymbol_ReturnsNotFound()
    {
        // Arrange
        var uri = "doc://NonExistent.Type.Name";

        // Act
        var result = DocumentationResource.GetDocumentation(_documentationService, uri);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.ContainsKey("found"));
        Assert.False(resultDict["found"].GetBoolean());
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
        Assert.True(resultDict.ContainsKey("uri"));
        Assert.Equal(uri, resultDict["uri"].GetString());
        Assert.True(resultDict.ContainsKey("packages"));
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
        Assert.True(resultDict.ContainsKey("found"));
        Assert.True(resultDict["found"].GetBoolean());
        Assert.True(resultDict.ContainsKey("packageId"));
        Assert.Equal("Newtonsoft.Json", resultDict["packageId"].GetString());
        Assert.True(resultDict.ContainsKey("versions"));
        Assert.True(resultDict["versions"].GetArrayLength() > 0);
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
        Assert.True(resultDict.ContainsKey("found"));
        Assert.False(resultDict["found"].GetBoolean());
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
        Assert.True(resultDict.ContainsKey("uri"));
        Assert.True(resultDict.ContainsKey("mimeType"));
        Assert.Equal("text/markdown", resultDict["mimeType"].GetString());
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
        Assert.True(resultDict.ContainsKey("uri"));
        Assert.True(resultDict.ContainsKey("mimeType"));
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
        Assert.True(resultDict.ContainsKey("uri"));
        Assert.Equal(uri, resultDict["uri"].GetString());
        Assert.True(resultDict.ContainsKey("frameworkVersion"));
        Assert.True(resultDict.ContainsKey("language"));
        Assert.True(resultDict.ContainsKey("capabilities"));
        Assert.True(resultDict.ContainsKey("defaultImports"));
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
        Assert.True(resultDict.ContainsKey("uri"));
        Assert.Equal(uri, resultDict["uri"].GetString());
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
        Assert.True(resultDict.ContainsKey("uri"));
        Assert.Equal(uri, resultDict["uri"].GetString());
        Assert.True(resultDict.ContainsKey("isSessionSpecific"));
        Assert.True(resultDict["isSessionSpecific"].GetBoolean());
        Assert.True(resultDict.ContainsKey("contextId"));
        Assert.Equal("test-context-123", resultDict["contextId"].GetString());
    }
}
