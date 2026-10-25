namespace Pricely.Modules.AiEngine.Domain.Models;

/// <summary>
/// Provider through which a model is called. The same model has its own identifier and price at each provider.
/// </summary>
public enum Provider
{
    Anthropic = 0
}

public static class Providers
{
    /// <summary>
    /// Provider of a call from the name reported by the LLM services (<c>Anthropic</c>, <c>VertexAi/Anthropic</c>...).
    /// Null when the name is missing or unknown.
    /// </summary>
    public static Provider? FromReportedName(string? reportedName)
    {
        if (string.IsNullOrWhiteSpace(reportedName))
        {
            return null;
        }

        string prefix = reportedName.Split('/')[0].Trim();

        // Names only: Enum.TryParse would also accept numeric values.
        bool isName = prefix.Length > 0 && prefix.All(char.IsAsciiLetter);

        return isName && Enum.TryParse(prefix, ignoreCase: true, out Provider provider) ? provider : null;
    }
}
