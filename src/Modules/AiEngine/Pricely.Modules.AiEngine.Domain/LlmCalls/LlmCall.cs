using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Domain.LlmCalls;

/// <summary>
/// A call to an LLM, immutable once recorded. Single source of the usage statistics.
/// </summary>
public sealed class LlmCall : Entity
{
    public const int ErrorMessageMaxLength = 2000;

    private LlmCall()
    {
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public DateTime CalledOnUtc { get; private set; }

    public string FeatureCode { get; private set; }

    public string ModelProvider { get; private set; }

    public string ModelName { get; private set; }

    public int InputTokens { get; private set; }

    public int InputCachedTokens { get; private set; }

    public int InputCacheCreationTokens { get; private set; }

    public int OutputTokens { get; private set; }

    public int OutputReasoningTokens { get; private set; }

    public long ResponseTimeMs { get; private set; }

    public bool IsSuccess { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>Catalog model matching the reported provider and model; null when the model is not in the catalog.</summary>
    public Guid? ModelId { get; private set; }

    /// <summary>Costs in USD, frozen at the price in force on the day of the call. Null when the call is not priced.</summary>
    public decimal? InputCost { get; private set; }

    public decimal? CachedInputCost { get; private set; }

    public decimal? CacheWriteCost { get; private set; }

    public decimal? OutputCost { get; private set; }

    public decimal? TotalCost { get; private set; }

    public int TotalTokens => InputTokens + InputCachedTokens + InputCacheCreationTokens + OutputTokens;

    /// <summary>
    /// Records a call and freezes its cost with the price of <paramref name="model"/> in force on the day of the call.
    /// Without a model, or without a price on that day, the call stays unpriced.
    /// </summary>
    public static LlmCall Record(Guid sessionId, LlmCallDetails details, Model? model)
    {
        LlmCall call = new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            CalledOnUtc = details.CalledOnUtc,
            FeatureCode = details.FeatureCode,
            ModelProvider = details.ModelProvider,
            ModelName = details.ModelName,
            InputTokens = details.TokenUsage.InputTokens,
            InputCachedTokens = details.TokenUsage.InputCachedTokens,
            InputCacheCreationTokens = details.TokenUsage.InputCacheCreationTokens,
            OutputTokens = details.TokenUsage.OutputTokens,
            OutputReasoningTokens = details.TokenUsage.OutputReasoningTokens,
            ResponseTimeMs = details.ResponseTimeMs,
            IsSuccess = details.IsSuccess,
            ErrorMessage = details.ErrorMessage is { Length: > ErrorMessageMaxLength } message
                ? message[..ErrorMessageMaxLength]
                : details.ErrorMessage
        };

        call.ApplyPricing(model);

        return call;
    }

    private void ApplyPricing(Model? model)
    {
        ModelPrice? price = model?.PriceAt(CalledOnUtc);
        if (model is null || price is null)
        {
            return;
        }

        CallCost cost = price.CostOf(new TokenUsage(
            InputTokens,
            InputCachedTokens,
            InputCacheCreationTokens,
            OutputTokens,
            OutputReasoningTokens));

        ModelId = model.Id;
        InputCost = cost.Input;
        CachedInputCost = cost.CachedInput;
        CacheWriteCost = cost.CacheWrite;
        OutputCost = cost.Output;
        TotalCost = cost.Total;
    }
}
