using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.WebSearches;

/// <summary>
/// A web search through a provider (ADR-027), immutable once recorded, with its cost frozen at the price per search in
/// force when it was recorded. The tokens an LLM-driven search spends are journaled apart, as an LLM call of the
/// <c>common.web-search</c> feature.
/// </summary>
public sealed class WebSearch : Entity
{
    public const int SessionIdMaxLength = 100;

    public const int ProviderMaxLength = 50;

    public const int UserIdMaxLength = 100;

    public const int ErrorMessageMaxLength = 2000;

    private WebSearch()
    {
    }

    public Guid Id { get; private set; }

    public string SessionId { get; private set; }

    public DateTime SearchedOnUtc { get; private set; }

    public string Provider { get; private set; }

    public string? UserId { get; private set; }

    /// <summary>Searches the provider billed for this request (an LLM-driven provider may run several).</summary>
    public int Searches { get; private set; }

    public int Results { get; private set; }

    public long ResponseTimeMs { get; private set; }

    public bool IsSuccess { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>Price of one search in USD when recorded; null when the provider has no configured price.</summary>
    public decimal? PricePerSearch { get; private set; }

    /// <summary>Cost in USD (searches × price per search); null when not priced.</summary>
    public decimal? Cost { get; private set; }

    public static WebSearch Record(WebSearchDetails details, decimal? pricePerSearch) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = Cut(details.SessionId, SessionIdMaxLength)!,
            SearchedOnUtc = details.SearchedOnUtc,
            Provider = Cut(details.Provider, ProviderMaxLength)!,
            UserId = Cut(details.UserId, UserIdMaxLength),
            Searches = Math.Max(0, details.Searches),
            Results = Math.Max(0, details.Results),
            ResponseTimeMs = Math.Max(0, details.ResponseTimeMs),
            IsSuccess = details.IsSuccess,
            ErrorMessage = Cut(details.ErrorMessage, ErrorMessageMaxLength),
            PricePerSearch = pricePerSearch,
            Cost = pricePerSearch * Math.Max(0, details.Searches)
        };

    private static string? Cut(string? value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;
}
