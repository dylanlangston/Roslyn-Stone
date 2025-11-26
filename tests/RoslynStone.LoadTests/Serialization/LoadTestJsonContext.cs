using System.Text.Json.Serialization;

namespace RoslynStone.LoadTests.Serialization;

/// <summary>
/// Source-generated JSON serialization context for load tests
/// Provides high-performance JSON serialization for MCP requests
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class LoadTestJsonContext : JsonSerializerContext;
