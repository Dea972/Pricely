using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pricely.Common.Application.JsonConverters;


public sealed class DecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String when decimal.TryParse(reader.GetString()?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d) => d,
            _ => throw new JsonException($"Invalid value for decimal: {reader.GetString()}")
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}

public sealed class NullableDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? input = reader.GetString();
        return decimal.TryParse(input?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)
            ? result
            : null;
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value?.ToString(CultureInfo.InvariantCulture));
}
