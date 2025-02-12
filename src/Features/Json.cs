using System.Text.Json.Serialization;

namespace Conesoft.Files;

public static class Json
{
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        RespectRequiredConstructorParameters = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
