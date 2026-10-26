using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Domain.LlmCalls;

/// <summary>
/// A call to an LLM, immutable once recorded. Single source of the usage statistics.
/// </summary>
public sealed class LlmCall : Entity
{
    public const int SessionIdMaxLength = 100;

    public const int FeatureCodeMaxLength = 100;

    public const int SourceModuleMaxLength = 50;

    public const int UserIdMaxLength = 100;

    public const int AgentNameMaxLength = 50;

    public const int ModelProviderMaxLength = 50;

    public const int ModelNameMaxLength = 150;

    public const int ErrorMessageMaxLength = 2000;

    private LlmCall()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Groups the calls of one unit of work (given by the caller); a new value per call when none is given.</summary>
    public string SessionId { get; private set; }

    public DateTime CalledOnUtc { get; private set; }

    public string FeatureCode { get; private set; }

    public string SourceModule { get; private set; }

    public string? UserId { get; private set; }

    /// <summary>Agent on whose behalf the call was made (Agentic module), if any.</summary>
    public string? AgentName { get; private set; }

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
    public static LlmCall Record(LlmCallDetails details, Model? model)
    {
        LlmCall call = new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = Truncate(details.SessionId, SessionIdMaxLength),
            CalledOnUtc = details.CalledOnUtc,
            FeatureCode = Truncate(details.FeatureCode, FeatureCodeMaxLength),
            SourceModule = Truncate(details.SourceModule, SourceModuleMaxLength),
            UserId = details.UserId is null ? null : Truncate(details.UserId, UserIdMaxLength),
            AgentName = details.AgentName is null ? null : Truncate(details.AgentName, AgentNameMaxLength),
            ModelProvider = Truncate(details.ModelProvider, ModelProviderMaxLength),
            ModelName = Truncate(details.ModelName, ModelNameMaxLength),
            InputTokens = details.TokenUsage.InputTokens,
            InputCachedTokens = details.TokenUsage.InputCachedTokens,
            InputCacheCreationTokens = details.TokenUsage.InputCacheCreationTokens,
            OutputTokens = details.TokenUsage.OutputTokens,
            OutputReasoningTokens = details.TokenUsage.OutputReasoningTokens,
            ResponseTimeMs = details.ResponseTimeMs,
            IsSuccess = details.IsSuccess,
            ErrorMessage = details.ErrorMessage is null ? null : Truncate(details.ErrorMessage, ErrorMessageMaxLength)
        };

        call.ApplyPricing(model);

        return call;
    }

    /// <summary>A journal entry is never lost because a reported value is too long: it is cut to the column size.</summary>
    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

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
