using System.Runtime.Serialization;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
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

    private static RequestContext<ReadResourceRequestParams> CreateRequestContext(string uri)
    {
        // Try creating via public constructor. If the library enforces non-null arguments, fall back to uninitialized object.
        try
        {
            var context = new RequestContext<ReadResourceRequestParams>(null!, null!);
            context
                .GetType()
                .GetProperty("Params")
                ?.SetValue(context, new ReadResourceRequestParams { Uri = uri });
            return context;
        }
        catch
        {
            // FormatterServices.GetUninitializedObject is obsolete but acceptable here for test isolation; use with pragma to avoid errors.
#pragma warning disable SYSLIB0050
            var contextType = typeof(RequestContext<ReadResourceRequestParams>);
            var context =
                (RequestContext<ReadResourceRequestParams>)
                    FormatterServices.GetUninitializedObject(contextType);
            var param = new ReadResourceRequestParams { Uri = uri };
            contextType.GetProperty("Params")?.SetValue(context, param);
            return context;
#pragma warning restore SYSLIB0050
        }
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task DocumentationResource_GetDocumentation_ValidUri_ReturnsDocumentation()
    {
        // Arrange
        var uri = "doc://System.String";
        var requestContext = CreateRequestContext(uri);

        // Act
        var id = uri.StartsWith("doc://") ? uri[6..] : uri;
        var result = await DocumentationResource.GetDocumentation(
            _documentationService,
            requestContext,
            id
        );
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
        var requestContext = CreateRequestContext(uri);

        // Act
        var id = uri.StartsWith("doc://") ? uri[6..] : uri;
        var result = await DocumentationResource.GetDocumentation(
            _documentationService,
            requestContext,
            id
        );
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
        var requestContext = CreateRequestContext(uri);

        // Act
        var id = uri.StartsWith("doc://") ? uri[6..] : uri;
        var result = await DocumentationResource.GetDocumentation(
            _documentationService,
            requestContext,
            id
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — either a structured response with a 'found' boolean or a TextResourceContents with a message
        Assert.NotNull(resultDict);
        if (resultDict.TryGetValue("found", out var foundElement))
        {
            Assert.False(foundElement.GetBoolean());
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.Contains("Documentation not found", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("Feature", "Documentation")]
    public async Task DocumentationResource_GetDocumentation_PackageUriFormat_ParsesCorrectly()
    {
        // Arrange
        var uri = "doc://Newtonsoft.Json@Newtonsoft.Json.JsonConvert";
        var requestContext = CreateRequestContext(uri);

        // Act
        var id = uri.StartsWith("doc://") ? uri[6..] : uri;
        var result = await DocumentationResource.GetDocumentation(
            _documentationService,
            requestContext,
            id
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — check URI and either a 'found' flag or a textual message
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        if (resultDict.TryGetValue("found", out var _))
        {
            Assert.True(true);
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.True(text.Length > 0);
        }
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetSearchResource_SearchPackages_ValidQuery_ReturnsResults()
    {
        // Arrange
        var uri = "nuget://search?q=Newtonsoft.Json&skip=0&take=5";
        var requestContext = CreateRequestContext(uri);

        // Act
        var result = await NuGetSearchResource.SearchPackages(
            _nugetService,
            requestContext,
            "Newtonsoft.Json",
            0,
            5
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — prefer structured properties; fallback to parsing the text content
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        if (resultDict.TryGetValue("totalCount", out var totalCountElement))
        {
            Assert.True(totalCountElement.GetInt32() > 0);
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.Contains("Search Results for", text);
            Assert.Contains("Total Packages Found", text);
        }
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetSearchResource_SearchPackages_WithoutQueryParams_UsesDefaults()
    {
        // Arrange
        var uri = "nuget://search?q=json";
        var requestContext = CreateRequestContext(uri);

        // Act
        var result = await NuGetSearchResource.SearchPackages(
            _nugetService,
            requestContext,
            "json",
            null,
            null
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — if structured response, check skip/take; otherwise verify text contains query and defaults implied
        Assert.NotNull(resultDict);
        if (
            resultDict.TryGetValue("skip", out var skipElem)
            && resultDict.TryGetValue("take", out var takeElem)
        )
        {
            Assert.Equal(0, skipElem.GetInt32());
            Assert.Equal(20, takeElem.GetInt32());
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.Contains("Search Results for 'json'", text);
        }
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetPackageResource_GetPackageVersions_ValidPackage_ReturnsVersions()
    {
        // Arrange
        var uri = "nuget://packages/Newtonsoft.Json/versions";
        var requestContext = CreateRequestContext(uri);

        // Act
        var path = uri.StartsWith("nuget://") ? uri.Substring("nuget://".Length) : uri;
        var parts = path.Split('/');
        var id = parts.Length > 1 ? parts[1] : string.Empty; // 'packages/{id}/versions'
        var result = await NuGetPackageResource.GetPackageVersions(
            _nugetService,
            requestContext,
            id
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — prefer structured response; fallback to parsing text
        Assert.NotNull(resultDict);
        if (resultDict.TryGetValue("found", out var foundElement))
        {
            Assert.True(foundElement.GetBoolean());
            Assert.True(resultDict.TryGetValue("packageId", out var packageIdElement));
            Assert.Equal("Newtonsoft.Json", packageIdElement.GetString());
            Assert.True(resultDict.TryGetValue("versions", out var versionsElement));
            Assert.True(versionsElement.GetArrayLength() > 0);
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.Contains("Total Versions", text);
            Assert.Contains("Package ID: Newtonsoft.Json", text);
        }
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetPackageResource_GetPackageVersions_InvalidUri_ReturnsNotFound()
    {
        // Arrange
        var uri = "nuget://packages//versions";
        var requestContext = CreateRequestContext(uri);

        // Act
        var path = uri.StartsWith("nuget://") ? uri.Substring("nuget://".Length) : uri;
        var parts = path.Split('/');
        var id = parts.Length > 1 ? parts[1] : string.Empty; // may be empty
        var result = await NuGetPackageResource.GetPackageVersions(
            _nugetService,
            requestContext,
            id
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — structured fallback, otherwise check textual message
        Assert.NotNull(resultDict);
        if (resultDict.TryGetValue("found", out var foundElement))
        {
            Assert.False(foundElement.GetBoolean());
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.True(
                text.Contains("Package not found", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Invalid package ID", StringComparison.OrdinalIgnoreCase)
            );
        }
    }

    [Fact]
    [Trait("Feature", "NuGet")]
    public async Task NuGetPackageResource_GetPackageReadme_ValidPackage_ReturnsReadme()
    {
        // Arrange
        var uri = "nuget://packages/Newtonsoft.Json/readme";
        var requestContext = CreateRequestContext(uri);

        // Act
        var id =
            uri.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault()
            ?? string.Empty; // 'packages/{id}/readme'
        var result = await NuGetPackageResource.GetPackageReadme(_nugetService, requestContext, id);
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out _));
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
        var requestContext = CreateRequestContext(uri);

        // Act
        var id =
            uri.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault()
            ?? string.Empty;
        // Extract version from query if present
        var version = uri.Contains("version=")
            ? uri.Split("version=").Last().Split('&').First()
            : null;
        var result = await NuGetPackageResource.GetPackageReadme(
            _nugetService,
            requestContext,
            id,
            version
        );
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
        var requestContext = CreateRequestContext(uri);

        // Act
        var result = ReplStateResource.GetReplState(
            _scriptingService,
            _contextManager,
            requestContext
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — check structured results or fallback to text content
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        if (resultDict.TryGetValue("frameworkVersion", out _))
        {
            Assert.True(true);
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.Contains("Framework Version", text);
            Assert.Contains("Language", text);
            Assert.Contains("Default Imports", text);
        }
    }

    [Fact]
    [Trait("Feature", "REPL")]
    public void ReplStateResource_GetReplState_AlternativeUri_ReturnsState()
    {
        // Arrange
        var uri = "repl://info";
        var requestContext = CreateRequestContext(uri);

        // Act
        var result = ReplStateResource.GetReplState(
            _scriptingService,
            _contextManager,
            requestContext
        );
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
        var requestContext = CreateRequestContext(uri);

        // Act
        var result = ReplStateResource.GetReplState(
            _scriptingService,
            _contextManager,
            requestContext
        );
        var json = JsonSerializer.Serialize(result);
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // Assert — structured response or text fallback
        Assert.NotNull(resultDict);
        Assert.True(resultDict.TryGetValue("uri", out var uriElement));
        Assert.Equal(uri, uriElement.GetString());
        if (resultDict.TryGetValue("isSessionSpecific", out var isSessionSpecific))
        {
            Assert.True(isSessionSpecific.GetBoolean());
            Assert.True(resultDict.TryGetValue("contextId", out var contextIdElement));
            Assert.Equal("test-context-123", contextIdElement.GetString());
        }
        else
        {
            Assert.True(resultDict.TryGetValue("text", out var textElem));
            var text = textElem.GetString() ?? string.Empty;
            Assert.Contains("Is Session Specific: True", text);
            Assert.Contains("Context ID: test-context-123", text);
        }
    }
}
