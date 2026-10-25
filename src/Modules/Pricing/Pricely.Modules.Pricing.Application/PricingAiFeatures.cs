using Pricely.Common.Application.AI;

namespace Pricely.Modules.Pricing.Application;

/// <summary>
/// AI features of the Pricing module, declared at startup and targetable in the AiEngine module.
/// </summary>
public static class PricingAiFeatures
{
    /// <summary>Collection of merchant offers by the AI agent (see ADR-008).</summary>
    public static readonly AiFeature OfferCollection = new(
        "pricing.offer-collection",
        "Collecte des offres des enseignes par l'agent",
        "Pricing",
        AiProviderKeys.Anthropic);
}
