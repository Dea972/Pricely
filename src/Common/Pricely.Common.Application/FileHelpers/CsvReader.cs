using System.Reflection;
using System.Text.Json;
using Pricely.Common.Application.JsonConverters;
namespace Pricely.Common.Application.FileHelpers;

public static class CsvReader
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = InitializeJsonSerializerOptions();

    private static JsonSerializerOptions InitializeJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new BoolStringConverter());
        options.Converters.Add(new NullableBoolStringConverter());
        options.Converters.Add(new NullableDecimalConverter());
        options.Converters.Add(new IntConverter());
        options.Converters.Add(new NullableIntConverter());
        options.Converters.Add(new DecimalConverter());
        options.Converters.Add(new SmartDateConverter());
        options.Converters.Add(new NullableSmartDateConverter());
        return options;
    }

    public static IEnumerable<T> Read<T>(
        string filePath,
        Dictionary<int, string>? columnMapping = null,
        char separator = ','
    ) where T : class, new()
    {
        ValidateParameters<T>(filePath, ref columnMapping);

        return ReadCsvIterator<T>(new StreamReader(filePath), columnMapping!, separator);
    }

    public static IEnumerable<T> Read<T>(
        Stream stream,
        Dictionary<int, string>? columnMapping = null,
        char separator = ';'
    ) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(stream);

        ValidateParameters<T>(null, ref columnMapping);

        return ReadCsvIterator<T>(new StreamReader(stream), columnMapping!, separator);
    }

    private static void ValidateParameters<T>(
        string? filePath,
        ref Dictionary<int, string>? columnMapping
    ) where T : class, new()
    {
        if (filePath != null && !File.Exists(filePath))
        {
            throw new FileNotFoundException("Le fichier CSV est introuvable.", filePath);
        }

        if (columnMapping is null)
        {
            PropertyInfo[] properties = typeof(T)
                .GetProperties()
                .Where(p => p.CanWrite 
                    && p.PropertyType != typeof(object)
                    && p.PropertyType != typeof(List<>))
                .ToArray();

            columnMapping = new Dictionary<int, string>();

            for (int i = 0; i < properties.Length; i++)
            {
                columnMapping[i] = properties[i].Name;
            }
        }
    }

    private static IEnumerable<T> ReadCsvIterator<T>(
        StreamReader reader,
        Dictionary<int, string> columnMapping,
        char separator
    ) where T : class, new()
    {
        using (reader)
        {
            int lineNumber = 0;

            while (!reader.EndOfStream)
            {
                lineNumber++;
                string? line;

                try
                {
                    line = reader.ReadLine();
                }
                catch (IOException ex)
                {
                    throw new InvalidOperationException($"Erreur de lecture à la ligne {lineNumber}.", ex);
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] values;
                try
                {
                    values = line.Split(separator);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Échec de séparation de la ligne {lineNumber}. Contenu : \"{line}\"", ex);
                }

                var jsonElements = new Dictionary<string, string>();

                foreach (KeyValuePair<int, string> pair in columnMapping)
                {
                    int index = pair.Key;
                    string propertyName = pair.Value;

                    if (index >= values.Length)
                    {
                        continue;
                    }

                    jsonElements[propertyName] = values[index].Trim();
                }

                string json;
                try
                {
                    json = JsonSerializer.Serialize(jsonElements, CachedJsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Échec de sérialisation JSON à la ligne {lineNumber}. Données : {string.Join(", ", jsonElements)}", ex);
                }

                T? obj;
                try
                {
                    obj = JsonSerializer.Deserialize<T>(json, CachedJsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Erreur de désérialisation à la ligne {lineNumber}. JSON : {json}", ex);
                }

                if (obj is not null)
                {
                    yield return obj;
                }
            }
        }
    }
}
