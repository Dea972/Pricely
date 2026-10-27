namespace Pricely.Modules.AiEngine.Domain.WebSearches;

/// <summary>What a searcher reports about one search request.</summary>
public sealed record WebSearchDetails
{
    public required string SessionId { get; init; }

    public required DateTime SearchedOnUtc { get; init; }

    public required string Provider { get; init; }

    public string? UserId { get; init; }

    public int Searches { get; init; }

    public int Results { get; init; }

    public long ResponseTimeMs { get; init; }

    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }
}
