using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pricely.Common.Application.JsonConverters;
public class NullableSmartDateConverter : JsonConverter<DateTime?>
{
    private static readonly string[] Formats = { "yyyy-MM-dd", "yyyyMMdd", "dd/MM/yyyy", "MM/dd/yyyy", "dd/MM/yyyy hh:mm:ss" };

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? input = reader.GetString();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // 1. Try known formats
        if (DateTime.TryParseExact(input, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exactMatch))
        {
            return exactMatch;
        }

        // 2. Fallback: flexible parsing
        if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime fallback))
        {
            return fallback;
        }

        throw new JsonException($"Invalid date format: {input}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

}


public class SmartDateConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats = { "yyyy-MM-dd", "yyyyMMdd", "dd/MM/yyyy", "MM/dd/yyyy","dd/MM/yyyy hh:mm:ss" };

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? input = reader.GetString();

        if (string.IsNullOrWhiteSpace(input))
        {
            return DateTime.Now;
        }

        // 1. Try known formats
        if (DateTime.TryParseExact(input, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exactMatch))
        {
            return exactMatch;
        }

        // 2. Fallback: flexible parsing
        if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime fallback))
        {
            return fallback;
        }


        throw new JsonException($"Invalid date format: {input}");
    }


    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
