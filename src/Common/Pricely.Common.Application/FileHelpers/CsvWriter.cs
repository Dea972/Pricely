using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Pricely.Common.Application.FileHelpers;

public static class CsvWriter
{

  public static Stream Write<T>(
        IEnumerable<T> data,
        Dictionary<int, string>? columnMapping = null,
        char separator = ';',
        bool includeHeader = false
    ) where T : class
    {
        var stream = new MemoryStream();

        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        using var writer = new StreamWriter(stream, utf8NoBom, leaveOpen: true);

        Write(data, writer, columnMapping, separator, includeHeader);

        stream.Position = 0;
        return stream;
    }

    public static void Write<T>(
        IEnumerable<T> data,
        string filePath,
        Dictionary<int, string>? columnMapping = null,
        char separator = ';',
        bool includeHeader = false
    ) where T : class
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        Write(data, writer, columnMapping, separator, includeHeader);
    }

    public static void Write<T>(
        IEnumerable<T> data,
        Stream stream,
        Dictionary<int, string>? columnMapping = null,
        char separator = ';',
        bool includeHeader = false
    ) where T : class
    {
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        Write(data, writer, columnMapping, separator, includeHeader);
    }

    private static void Write<T>(
        IEnumerable<T> data,
        StreamWriter writer,
        Dictionary<int, string>? columnMapping,
        char separator,
        bool includeHeader
    ) where T : class
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        // Génération de la correspondance colonne/propriété si absente
        if (columnMapping is null)
        {
            PropertyInfo[] properties = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();
            columnMapping = new Dictionary<int, string>();
            for (int i = 0; i < properties.Length; i++)
            {
                columnMapping[i] = properties[i].Name;
            }
        }

        var propertyMap = columnMapping
            .ToDictionary(pair => pair.Key, pair => typeof(T).GetProperty(pair.Value)!);

        if (includeHeader)
        {
            IEnumerable<string> headers = columnMapping.OrderBy(kv => kv.Key).Select(kv => kv.Value);
            writer.WriteLine(string.Join(separator, headers));
        }

        foreach (T item in data)
        {
            IEnumerable<string> values = propertyMap
                .OrderBy(kv => kv.Key)
                .Select(kv =>
                {
                    object? value = kv.Value.GetValue(item);
                    return value?.ToString()?.Replace($"{separator}", $"\\{separator}") ?? "";
                });

            writer.WriteLine(string.Join(separator, values));
        }

        writer.Flush();
    }
}
