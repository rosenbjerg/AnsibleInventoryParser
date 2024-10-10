using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnsibleInventoryParser.Converters;

internal class BooleanConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(bool);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new BooleanConverter();
    }
}