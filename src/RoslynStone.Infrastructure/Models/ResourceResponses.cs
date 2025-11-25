using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace RoslynStone.Infrastructure.Models;

/// <summary>
/// Response for documentation resource requests
/// </summary>
public class DocumentationResponse
{
    /// <summary>Resource URI</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>Whether documentation was found</summary>
    [JsonPropertyName("found")]
    public required bool Found { get; init; }

    /// <summary>MIME type</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Symbol name</summary>
    [JsonPropertyName("symbolName")]
    public string? SymbolName { get; init; }

    /// <summary>Summary documentation</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    /// <summary>Remarks documentation</summary>
    [JsonPropertyName("remarks")]
    public string? Remarks { get; init; }

    /// <summary>Parameter documentation</summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, string>? Parameters { get; init; }

    /// <summary>Return value documentation</summary>
    [JsonPropertyName("returns")]
    public string? Returns { get; init; }

    /// <summary>Exception documentation</summary>
    [JsonPropertyName("exceptions")]
    public List<string>? Exceptions { get; init; }

    /// <summary>Example documentation</summary>
    [JsonPropertyName("example")]
    public string? Example { get; init; }

    /// <summary>Error or informational message</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Implicit conversion to TextResourceContents
    /// </summary>
    /// <param name="docResponse"></param>
    public static implicit operator TextResourceContents(DocumentationResponse docResponse)
    {
        return new TextResourceContents
        {
            Uri = docResponse.Uri,
            MimeType = docResponse.MimeType,
            Text = docResponse.Found
                ? $"Symbol: {docResponse.SymbolName}\n\nSummary: {docResponse.Summary}\n\nRemarks: {docResponse.Remarks}\n\nParameters: {string.Join(", ", docResponse.Parameters ?? new())}\n\nReturns: {docResponse.Returns}\n\nExceptions: {string.Join(", ", docResponse.Exceptions ?? new())}\n\nExample: {docResponse.Example}"
                : docResponse.Message ?? "Documentation not found.",
        };
    }
}

/// <summary>
/// Response for package search resource requests
/// </summary>
public class PackageSearchResponse
{
    /// <summary>Resource URI</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>MIME type</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Package search results</summary>
    [JsonPropertyName("packages")]
    public required List<PackageInfo> Packages { get; init; }

    /// <summary>Total result count</summary>
    [JsonPropertyName("totalCount")]
    public required int TotalCount { get; init; }

    /// <summary>Search query</summary>
    [JsonPropertyName("query")]
    public required string Query { get; init; }

    /// <summary>Results to skip</summary>
    [JsonPropertyName("skip")]
    public required int Skip { get; init; }

    /// <summary>Results to take</summary>
    [JsonPropertyName("take")]
    public required int Take { get; init; }

    /// <summary>
    /// Implicit conversion to TextResourceContents
    /// </summary>
    /// <param name="searchResponse"></param>
    public static implicit operator TextResourceContents(PackageSearchResponse searchResponse)
    {
        var contentLines = new List<string>
        {
            $"Search Results for '{searchResponse.Query}':",
            $"Total Packages Found: {searchResponse.TotalCount}",
            "",
        };

        foreach (var pkg in searchResponse.Packages)
        {
            contentLines.Add($"Package ID: {pkg.Id}");
            contentLines.Add($"Title: {pkg.Title}");
            contentLines.Add($"Description: {pkg.Description}");
            contentLines.Add($"Authors: {pkg.Authors}");
            contentLines.Add($"Latest Version: {pkg.LatestVersion}");
            contentLines.Add($"Download Count: {pkg.DownloadCount}");
            contentLines.Add($"Icon URL: {pkg.IconUrl}");
            contentLines.Add($"Project URL: {pkg.ProjectUrl}");
            contentLines.Add($"Tags: {pkg.Tags}");
            contentLines.Add(new string('-', 40));
        }

        return new TextResourceContents
        {
            Uri = searchResponse.Uri,
            MimeType = "application/json",
            Text = string.Join("\n", contentLines),
        };
    }
}

/// <summary>
/// Package information in search results
/// </summary>
public class PackageInfo
{
    /// <summary>Package ID</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Package title</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Package description</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Package authors</summary>
    [JsonPropertyName("authors")]
    public string? Authors { get; init; }

    /// <summary>Latest version</summary>
    [JsonPropertyName("latestVersion")]
    public string? LatestVersion { get; init; }

    /// <summary>Total download count</summary>
    [JsonPropertyName("downloadCount")]
    public long DownloadCount { get; init; }

    /// <summary>Icon URL</summary>
    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; init; }

    /// <summary>Project URL</summary>
    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; init; }

    /// <summary>Package tags</summary>
    [JsonPropertyName("tags")]
    public string? Tags { get; init; }

    /// <summary>
    /// Implicit conversion to TextResourceContents
    /// </summary>
    /// <param name="pkgInfo"></param>
    public static implicit operator TextResourceContents(PackageInfo pkgInfo)
    {
        return new TextResourceContents
        {
            Uri = $"nuget://packages/{pkgInfo.Id}",
            MimeType = "application/json",
            Text = $"Package ID: {pkgInfo.Id}\nTitle: {pkgInfo.Title}\nDescription: {pkgInfo.Description}\nAuthors: {pkgInfo.Authors}\nLatest Version: {pkgInfo.LatestVersion}\nDownload Count: {pkgInfo.DownloadCount}\nIcon URL: {pkgInfo.IconUrl}\nProject URL: {pkgInfo.ProjectUrl}\nTags: {pkgInfo.Tags}",
        };
    }
}

/// <summary>
/// Response for package versions resource requests
/// </summary>
public class PackageVersionsResponse
{
    /// <summary>Resource URI</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>MIME type</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Whether package was found</summary>
    [JsonPropertyName("found")]
    public required bool Found { get; init; }

    /// <summary>Package ID</summary>
    [JsonPropertyName("packageId")]
    public string? PackageId { get; init; }

    /// <summary>Package versions</summary>
    [JsonPropertyName("versions")]
    public List<PackageVersionInfo>? Versions { get; init; }

    /// <summary>Total version count</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    /// <summary>Error or informational message</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Implicit conversion to TextResourceContents
    /// </summary>
    /// <param name="versionsResponse"></param>
    public static implicit operator TextResourceContents(PackageVersionsResponse versionsResponse)
    {
        if (!versionsResponse.Found || versionsResponse.Versions == null)
        {
            return new TextResourceContents
            {
                Uri = versionsResponse.Uri,
                MimeType = "application/json",
                Text = versionsResponse.Message ?? "Package not found.",
            };
        }

        var contentLines = new List<string>
        {
            $"Package ID: {versionsResponse.PackageId}",
            $"Total Versions: {versionsResponse.TotalCount}",
            "",
        };

        foreach (var version in versionsResponse.Versions)
        {
            contentLines.Add($"Version: {version.Version}");
            contentLines.Add($"Download Count: {version.DownloadCount}");
            contentLines.Add($"Is Prerelease: {version.IsPrerelease}");
            contentLines.Add($"Is Deprecated: {version.IsDeprecated}");
            contentLines.Add(new string('-', 30));
        }

        return new TextResourceContents
        {
            Uri = versionsResponse.Uri,
            MimeType = "application/json",
            Text = string.Join("\n", contentLines),
        };
    }
}

/// <summary>
/// Package version information
/// </summary>
public class PackageVersionInfo
{
    /// <summary>Version string</summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>Download count for this version</summary>
    [JsonPropertyName("downloadCount")]
    public long DownloadCount { get; init; }

    /// <summary>Whether this is a prerelease version</summary>
    [JsonPropertyName("isPrerelease")]
    public required bool IsPrerelease { get; init; }

    /// <summary>Whether this version is deprecated</summary>
    [JsonPropertyName("isDeprecated")]
    public required bool IsDeprecated { get; init; }

    /// <summary>
    /// Implicit conversion to TextResourceContents
    /// </summary>
    /// <param name="versionInfo"></param>
    public static implicit operator TextResourceContents(PackageVersionInfo versionInfo)
    {
        return new TextResourceContents
        {
            Uri = $"nuget://packages/{{packageId}}/versions/{versionInfo.Version}",
            MimeType = "application/json",
            Text = $"Version: {versionInfo.Version}\nDownload Count: {versionInfo.DownloadCount}\nIs Prerelease: {versionInfo.IsPrerelease}\nIs Deprecated: {versionInfo.IsDeprecated}",
        };
    }
}

/// <summary>
/// Response for package README resource requests
/// </summary>
public class PackageReadmeResponse
{
    /// <summary>Resource URI</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>MIME type</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Whether README was found</summary>
    [JsonPropertyName("found")]
    public required bool Found { get; init; }

    /// <summary>Package ID</summary>
    [JsonPropertyName("packageId")]
    public string? PackageId { get; init; }

    /// <summary>Package version</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>README content</summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>Error or informational message</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Implicit conversion to TextResourceContents
    /// </summary>
    /// <param name="readmeResponse"></param>

    public static implicit operator TextResourceContents(PackageReadmeResponse readmeResponse)
    {
        return new TextResourceContents
        {
            Uri = readmeResponse.Uri,
            MimeType = readmeResponse.MimeType,
            Text = readmeResponse.Found
                ? readmeResponse.Content ?? "No README content available."
                : readmeResponse.Message ?? "README not found.",
        };
    }
}

/// <summary>
/// Response for REPL state resource requests
/// </summary>
public class ReplStateResponse
{
    /// <summary>Resource URI</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>MIME type</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Framework version</summary>
    [JsonPropertyName("frameworkVersion")]
    public required string FrameworkVersion { get; init; }

    /// <summary>Programming language</summary>
    [JsonPropertyName("language")]
    public required string Language { get; init; }

    /// <summary>REPL state</summary>
    [JsonPropertyName("state")]
    public required string State { get; init; }

    /// <summary>Active session count</summary>
    [JsonPropertyName("activeSessionCount")]
    public required int ActiveSessionCount { get; init; }

    /// <summary>Context ID for session-specific requests</summary>
    [JsonPropertyName("contextId")]
    public string? ContextId { get; init; }

    /// <summary>Whether this is session-specific state</summary>
    [JsonPropertyName("isSessionSpecific")]
    public required bool IsSessionSpecific { get; init; }

    /// <summary>Default namespace imports</summary>
    [JsonPropertyName("defaultImports")]
    public required List<string> DefaultImports { get; init; }

    /// <summary>REPL capabilities</summary>
    [JsonPropertyName("capabilities")]
    public required ReplCapabilities Capabilities { get; init; }

    /// <summary>Usage tips</summary>
    [JsonPropertyName("tips")]
    public required List<string> Tips { get; init; }

    /// <summary>Code examples</summary>
    [JsonPropertyName("examples")]
    public required ReplExamples Examples { get; init; }

    /// <summary>Session metadata for session-specific requests</summary>
    [JsonPropertyName("sessionMetadata")]
    public SessionMetadata? SessionMetadata { get; init; }

    /// <summary>
    /// Implicit conversion to TextResourceContents
    /// </summary>
    /// <param name="replState"></param>
    public static implicit operator TextResourceContents(ReplStateResponse replState)
    {
        var contentLines = new List<string>
        {
            $"Framework Version: {replState.FrameworkVersion}",
            $"Language: {replState.Language}",
            $"State: {replState.State}",
            $"Active Session Count: {replState.ActiveSessionCount}",
            $"Context ID: {replState.ContextId}",
            $"Is Session Specific: {replState.IsSessionSpecific}",
            $"Default Imports: {string.Join(", ", replState.DefaultImports)}",
            $"Capabilities: {JsonSerializer.Serialize(replState.Capabilities)}",
            $"Tips: {string.Join("\n", replState.Tips)}",
            $"Examples: {JsonSerializer.Serialize(replState.Examples)}",
            $"Session Metadata: {JsonSerializer.Serialize(replState.SessionMetadata)}",
        };

        return new TextResourceContents
        {
            Uri = replState.Uri,
            MimeType = "application/json",
            Text = string.Join("\n", contentLines),
        };
    }
}

/// <summary>
/// REPL capabilities
/// </summary>
public class ReplCapabilities
{
    /// <summary>Async/await support</summary>
    [JsonPropertyName("asyncAwait")]
    public required bool AsyncAwait { get; init; }

    /// <summary>LINQ support</summary>
    [JsonPropertyName("linq")]
    public required bool Linq { get; init; }

    /// <summary>Top-level statements support</summary>
    [JsonPropertyName("topLevelStatements")]
    public required bool TopLevelStatements { get; init; }

    /// <summary>Console output capture</summary>
    [JsonPropertyName("consoleOutput")]
    public required bool ConsoleOutput { get; init; }

    /// <summary>NuGet package loading</summary>
    [JsonPropertyName("nugetPackages")]
    public required bool NugetPackages { get; init; }

    /// <summary>Stateful execution</summary>
    [JsonPropertyName("statefulness")]
    public required bool Statefulness { get; init; }
}

/// <summary>
/// REPL usage examples
/// </summary>
public class ReplExamples
{
    /// <summary>Simple expression example</summary>
    [JsonPropertyName("simpleExpression")]
    public required string SimpleExpression { get; init; }

    /// <summary>Variable declaration example</summary>
    [JsonPropertyName("variableDeclaration")]
    public required string VariableDeclaration { get; init; }

    /// <summary>Async operation example</summary>
    [JsonPropertyName("asyncOperation")]
    public required string AsyncOperation { get; init; }

    /// <summary>LINQ query example</summary>
    [JsonPropertyName("linqQuery")]
    public required string LinqQuery { get; init; }

    /// <summary>Console output example</summary>
    [JsonPropertyName("consoleOutput")]
    public required string ConsoleOutput { get; init; }
}

/// <summary>
/// Session metadata for session-specific REPL state
/// </summary>
public class SessionMetadata
{
    /// <summary>Context ID</summary>
    [JsonPropertyName("contextId")]
    public required string ContextId { get; init; }

    /// <summary>Session creation time</summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Last access time</summary>
    [JsonPropertyName("lastAccessedAt")]
    public required DateTimeOffset LastAccessedAt { get; init; }

    /// <summary>Number of executions</summary>
    [JsonPropertyName("executionCount")]
    public required int ExecutionCount { get; init; }

    /// <summary>Whether session is initialized</summary>
    [JsonPropertyName("isInitialized")]
    public required bool IsInitialized { get; init; }
}
