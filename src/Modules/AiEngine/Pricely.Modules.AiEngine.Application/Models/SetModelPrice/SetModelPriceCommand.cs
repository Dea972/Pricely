using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Models.SetModelPrice;

/// <summary>Sets the price of a model (USD per million tokens) from a UTC day; replaces the price already set on that day.</summary>
public sealed record SetModelPriceCommand(
    Guid ModelId,
    DateOnly ValidFrom,
    decimal InputPerMillion,
    decimal CachedInputPerMillion,
    decimal CacheWritePerMillion,
    decimal OutputPerMillion) : ICommand;
