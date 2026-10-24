using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;

namespace Pricely.Common.Application.JsonConverters;

public sealed class BoolStringConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString()?.ToUpper(CultureInfo.CurrentCulture) == "O";
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteStringValue(value ? "O" : "N");
}

public class NullableBoolStringConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString()?.ToUpperInvariant() switch
        {
            "O" => true,
            "N" => false,
            _ => null
        };

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        string? stringValue = value switch
        {
            null => null,
            _ => value.Value ? "O" : "N",
        };
        writer.WriteStringValue(stringValue);
    }
}
