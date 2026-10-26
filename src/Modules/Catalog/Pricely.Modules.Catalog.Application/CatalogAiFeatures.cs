using Pricely.Common.Application.AI;

namespace Pricely.Modules.Catalog.Application;

/// <summary>
/// AI features of the Catalog module, declared at startup and targetable in the AiEngine module.
/// </summary>
public static class CatalogAiFeatures
{
    /// <summary>Entry of a reference product from a product page (ADR-020).</summary>
    public static readonly AiFeature ProductEntry = new(
        "catalog.product-entry",
        "Saisie des produits assistée par l'IA",
        "Catalog",
        AiProviderKeys.Anthropic);
}
