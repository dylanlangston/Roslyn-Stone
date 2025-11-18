using System.Text.Json.Serialization;

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
