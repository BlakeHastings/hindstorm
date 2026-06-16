using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hindstorm;

/// <summary>
/// Serializes a <see cref="DomainModel"/> to JSON: the raw nodes and edges, for a downstream tool or a
/// future visualizer to consume. Enums are written as their names.
/// </summary>
public static class JsonExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Renders the model as indented JSON.</summary>
    public static string Export(DomainModel model) => JsonSerializer.Serialize(model, Options);
}
