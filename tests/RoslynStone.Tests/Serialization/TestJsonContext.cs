using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynStone.Tests.Serialization;

/// <summary>
/// Source-generated JSON serialization context for RoslynStone tests
/// Provides high-performance JSON serialization for test scenarios
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(object))]
public partial class TestJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Helper to serialize dynamic tool results using reflection-based serialization
    /// (source generators can't handle anonymous types)
    /// </summary>
    public static string SerializeDynamic(object value)
    {
        return JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Helper to deserialize to Dictionary using source-generated context
    /// </summary>
    public static Dictionary<string, JsonElement>? DeserializeToDictionary(string json)
    {
        return JsonSerializer.Deserialize(json, Default.DictionaryStringJsonElement);
    }
}
