using Pricely.Common.Application.EventBus;

namespace Pricely.Common.Application.Web;

/// <summary>
/// Published after every web search (success or failure) by the searcher. Consumed by the AiEngine module to keep the
/// search journal and its cost per provider; the tokens of an LLM-driven search are journaled apart, as an LLM call of
/// <see cref="WebSearchFeatures.Search"/>.
/// </summary>
public sealed class WebSearchPerformedIntegrationEvent : IntegrationEvent
{
    public WebSearchPerformedIntegrationEvent(Guid id, DateTime occurredOnUtc)
        : base(id, occurredOnUtc)
    {
    }

    public required string Provider { get; init; }

    public required string SessionId { get; init; }

    public string? UserId { get; init; }

    /// <summary>Billed searches (0 when the provider failed before searching).</summary>
    public int Searches { get; init; }

    public int Results { get; init; }

    public long ResponseTimeMs { get; init; }

    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }
}
