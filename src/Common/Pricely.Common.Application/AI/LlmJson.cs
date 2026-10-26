using System.Text.Json;

namespace Pricely.Common.Application.AI;

/// <summary>
/// Reads the JSON answer of a model (<see cref="LlmPrompt.RequireJsonResponse"/>): the object between the first <c>{</c>
/// and the last <c>}</c>, so that a code fence or a sentence around it does not matter. Null when it cannot be read.
/// </summary>
public static class LlmJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T? Read<T>(string content)
        where T : class
    {
        int start = content.IndexOf('{', StringComparison.Ordinal);
        int end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content[start..(end + 1)], Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
