using Pricely.Common.Application.AI;
using Pricely.Common.Domain;

namespace Pricely.Common.Application.Web;

/// <summary>
/// Port to a web search engine (ADR-027): a query gives a list of pages, never a verified price. The provider is chosen by
/// configuration (<c>WebSearch:Provider</c>, Anthropic or Brave) so that their cost and quality can be compared; every
/// search is journaled (<see cref="WebSearchPerformedIntegrationEvent"/>) and capped per day. Implemented in
/// Common.Infrastructure.
/// </summary>
public interface IWebSearcher
{
    Task<Result<WebSearchResults>> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default);
}

/// <param name="Query">What to search, in the words of the user.</param>
public sealed record WebSearchRequest(string Query)
{
    public const int MaxQueryLength = 400;

    /// <summary>Results wanted (the provider may return fewer).</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>ISO 3166-1 alpha-2 country the results are localized for.</summary>
    public string Country { get; init; } = "FR";

    /// <summary>The conversation or the job the search belongs to, for the journal.</summary>
    public string? SessionId { get; init; }

    /// <summary>The user on whose behalf the search runs, for the journal.</summary>
    public string? UserId { get; init; }
}

/// <param name="Provider">The search provider (see <see cref="WebSearchProviders"/>).</param>
/// <param name="Results">The pages found, most relevant first, without duplicates.</param>
/// <param name="Searches">Billed searches the provider ran (an LLM-driven provider can run several).</param>
public sealed record WebSearchResults(string Provider, IReadOnlyList<WebSearchResult> Results, int Searches);

/// <param name="Title">Title of the page.</param>
/// <param name="Address">Address of the page.</param>
/// <param name="Snippet">An extract of the page, when the provider gives one (never verified by Pricely).</param>
/// <param name="PageAge">How old the page is, as the provider writes it, if known.</param>
public sealed record WebSearchResult(string Title, Uri Address, string? Snippet, string? PageAge);

public static class WebSearchProviders
{
    public const string Anthropic = "Anthropic";

    public const string Brave = "Brave";
}

public static class WebSearchFeatures
{
    /// <summary>
    /// The model that runs the Anthropic web search (the search itself is billed per search, its reading in tokens):
    /// its target can be changed in AiEngine like any feature.
    /// </summary>
    public static readonly AiFeature Search = new("common.web-search", "Recherche web (Anthropic)", "Common", AiProviderKeys.Anthropic);
}

public static class WebSearchErrors
{
    public static readonly Error EmptyQuery = Error.Problem("WebSearch.EmptyQuery", "The search text is required");

    public static Error DailyLimitReached(int limit) =>
        Error.Problem("WebSearch.DailyLimitReached", $"The {limit} web searches of the day are used up; try again tomorrow (UTC)");

    public static Error NotConfigured(string provider) =>
        Error.Problem("WebSearch.NotConfigured", $"The web search provider {provider} is not configured (API key)");

    public static Error Failed(string provider) =>
        Error.Problem("WebSearch.Failed", $"The web search through {provider} failed; the details are in the journal of AiEngine");
}
