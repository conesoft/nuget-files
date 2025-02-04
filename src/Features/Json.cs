using System.Text.Json;
using System.Text.Json.Serialization;

namespace Conesoft.Files;

public static class Json
{
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
