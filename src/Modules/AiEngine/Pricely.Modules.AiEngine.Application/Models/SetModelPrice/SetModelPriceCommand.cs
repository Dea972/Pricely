using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Models.SetModelPrice;

/// <summary>Sets the price of a model (USD per million tokens) from a UTC day; replaces the price already set on that day.</summary>
public sealed record SetModelPriceCommand(
    Guid ModelId,
    DateOnly ValidFrom,
    decimal InputPerMillion,
    decimal CachedInputPerMillion,
    decimal CacheWritePerMillion,
    decimal OutputPerMillion) : ICommand
{
    /// <summary>
    /// Where the price comes from: <c>Manual</c> (default), or the proposal of LiteLLM (<c>LiteLlm</c>) or of the AI
    /// (<c>AiReading</c>) that the user applied.
    /// </summary>
    public string? Source { get; init; }
}
