using System.Text.Json;
using System.Text.Json.Serialization;
using RoslynStone.Infrastructure.Models;

namespace RoslynStone.Infrastructure.Serialization;

/// <summary>
/// Source-generated JSON serialization context for RoslynStone
/// Provides high-performance, AOT-friendly JSON serialization
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(DocumentationResponse))]
[JsonSerializable(typeof(PackageSearchResponse))]
[JsonSerializable(typeof(PackageInfo))]
[JsonSerializable(typeof(PackageVersionsResponse))]
[JsonSerializable(typeof(PackageVersionInfo))]
[JsonSerializable(typeof(PackageReadmeResponse))]
[JsonSerializable(typeof(ExecutionStateResponse))]
[JsonSerializable(typeof(ReplCapabilities))]
[JsonSerializable(typeof(ReplExamples))]
[JsonSerializable(typeof(SessionMetadata))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<PackageInfo>))]
[JsonSerializable(typeof(List<PackageVersionInfo>))]
public partial class RoslynStoneJsonContext : JsonSerializerContext;
