namespace Pricely.Modules.AiEngine.Application.Models.ProposeModelPrices;

/// <summary>The JSON answer of the model of <see cref="AiEngineAiFeatures.PriceReading"/>.</summary>
internal sealed record PriceReadingReply(IReadOnlyList<ReadPrice>? Prices);

/// <summary>USD per million tokens, as the pricing page shows them.</summary>
internal sealed record ReadPrice(string? Model, decimal? Input, decimal? CachedInput, decimal? CacheWrite, decimal? Output);
