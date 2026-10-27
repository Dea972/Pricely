namespace Pricely.Common.Infrastructure.Web.Search;

/// <summary>
/// Web search settings (section <c>WebSearch</c>, ADR-027). The Brave key comes from user-secrets or the environment
/// (<c>WebSearch__Brave__ApiKey</c>), never from the repository.
/// </summary>
public sealed class WebSearchOptions
{
    public const string SectionName = "WebSearch";

    /// <summary><c>Anthropic</c> (the default) or <c>Brave</c>: change it to compare their cost and results.</summary>
    public string Provider { get; init; } = "Anthropic";

    /// <summary>Billed searches allowed per day (UTC) for the whole application; 0 turns web search off.</summary>
    public int MaxSearchesPerDay { get; init; } = 200;

    /// <summary>Searches an LLM-driven provider may run for one request (Anthropic <c>max_uses</c>).</summary>
    public int MaxSearchesPerRequest { get; init; } = 2;

    public BraveOptions Brave { get; init; } = new();
}

public sealed class BraveOptions
{
    public Uri BaseAddress { get; init; } = new("https://api.search.brave.com/");

    public string ApiKey { get; init; } = string.Empty;
}
