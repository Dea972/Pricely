using Pricely.Common.Application.AI;

namespace Pricely.Modules.AiEngine.Application;

/// <summary>AI features of the AiEngine module.</summary>
public static class AiEngineAiFeatures
{
    /// <summary>Reading of the prices of a model on the pricing page of its provider, when LiteLLM does not have them (ADR-021).</summary>
    public static readonly AiFeature PriceReading = new(
        "aiengine.price-reading",
        "Lecture des tarifs des modèles par l'IA",
        "AiEngine",
        AiProviderKeys.Anthropic);
}
