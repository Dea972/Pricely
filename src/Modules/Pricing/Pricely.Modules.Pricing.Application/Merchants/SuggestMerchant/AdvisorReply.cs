namespace Pricely.Modules.Pricing.Application.Merchants.SuggestMerchant;

/// <summary>The JSON answer of the model of <see cref="PricingAiFeatures.MerchantSetup"/>.</summary>
internal sealed record AdvisorReply(
    string? Name,
    string? SearchTemplate,
    string? TestQuery,
    IReadOnlyList<string>? CategoryIds,
    IReadOnlyDictionary<string, string>? CategoryReasons,
    string? Rationale);
