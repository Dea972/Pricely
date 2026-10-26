using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Pricely.Modules.Pricing.Presentation.Tools;

/// <summary>
/// MCP prompts of the Pricing module: the collection procedure of the collection agent (ADR-017), for an MCP client
/// that collects with the tools of Pricely.
/// </summary>
[McpServerPromptType]
public static class PricingPrompts
{
    [McpServerPrompt(Name = "collect_product_prices", Title = "Collecter les prix d'un produit")]
    [Description("Procédure de collecte des prix d'un produit chez les enseignes de son plan, avec les tools de Pricely.")]
    public static string CollectProductPrices([Description("Identifiant du produit.")] Guid productId) =>
        $"""
        Collecte les prix du produit {productId} avec les tools de Pricely :
        1. get_collection_plan : le produit de référence, les enseignes à interroger par priorité et les instructions de
           collecte de sa typologie (à suivre).
        2. Pour chaque enseigne : fetch_merchant_page avec searchText (sa page de recherche) ou une adresse de son site,
           puis la page du produit. Ne lis que les sites des enseignes du plan.
        3. submit_collected_offer avec le snapshotId de la page du produit : le prix affiché sur cette page, toutes taxes
           comprises, pour le même conditionnement que le produit de référence ; recopie le titre, le GTIN, la marque et
           la référence fabricant affichés, sans les corriger ; une similarité entre 0 et 1 et une justification courte.
        4. Si une enseigne ne vend pas le produit, ou seulement un lot ou un autre format, ne transmets rien pour elle.
        Termine par un résumé d'une ligne par enseigne. Tu ne confirmes jamais une offre : un humain la valide.
        """;
}
