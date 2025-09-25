using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnsibleInventoryParser.Converters;

internal class BooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var data = reader.GetString()!.ToLowerInvariant();
        return data is "true" or "yes";
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}