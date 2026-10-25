using System.ComponentModel;
using MediatR;
using ModelContextProtocol.Server;
using Pricely.Common.Presentation.Mcp;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;
using Pricely.Modules.Pricing.Application.Offers.GetCollectionPlan;
using Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;
using Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;
using Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;
using Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;

namespace Pricely.Modules.Pricing.Presentation.Tools;

/// <summary>
/// Pricing tools for AI agents (MCP and internal agent). Read-only for now; write tools (offer submission, review)
/// will require approval (ADR-008, ADR-009).
/// </summary>
[McpServerToolType]
public static class PricingTools
{
    [McpServerTool(Name = "list_merchants", Title = "Enseignes", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Liste les enseignes suivies (identifiant, nom, site web, active ou non).")]
    public static async Task<IReadOnlyCollection<MerchantResponse>> ListMerchants(
        ISender sender,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetMerchantsQuery(), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "compare_prices", Title = "Comparatif de prix", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("""
        Compare les prix d'un produit entre enseignes : offres confirmées avec leur dernier prix relevé, du moins cher
        au plus cher, avec disponibilité, date du relevé, page de l'enseigne et indicateur de prix suspect.
        Les offres proposées mais pas encore validées ne sont pas comparées.
        """)]
    public static async Task<IReadOnlyCollection<MerchantPriceResponse>> ComparePrices(
        ISender sender,
        [Description("Identifiant du produit (voir search_products).")] Guid productId,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetProductPriceComparisonQuery(productId), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "get_price_history", Title = "Historique de prix", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Historique des prix relevés d'un produit sur ses offres confirmées, du plus récent au plus ancien, par enseigne.")]
    public static async Task<IReadOnlyCollection<PriceHistoryEntryResponse>> GetPriceHistory(
        ISender sender,
        [Description("Identifiant du produit.")] Guid productId,
        [Description("Début de période (UTC, inclus), facultatif.")] DateTime? fromUtc = null,
        [Description("Fin de période (UTC, exclue), facultative.")] DateTime? toUtc = null,
        [Description("Nombre maximum de relevés (1 à 500, défaut 100).")] int limit = 100,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetPriceHistoryQuery(productId, fromUtc, toUtc, Math.Clamp(limit, 1, GetPriceHistoryQuery.MaxLimit)),
            cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "list_price_anomalies", Title = "Prix suspects", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("""
        Relevés de prix signalés comme suspects : écart de plus de 40 % avec la médiane des autres offres confirmées
        du produit. Souvent un mauvais rapprochement ou un conditionnement différent (lot contre unité).
        """)]
    public static async Task<IReadOnlyCollection<PriceAnomalyResponse>> ListPriceAnomalies(
        ISender sender,
        [Description("Depuis cette date (UTC), facultatif.")] DateTime? sinceUtc = null,
        [Description("Nombre maximum de résultats (1 à 200, défaut 50).")] int limit = 50,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetPriceAnomaliesQuery(sinceUtc, Math.Clamp(limit, 1, GetPriceAnomaliesQuery.MaxLimit)),
            cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "get_collection_plan", Title = "Plan de collecte", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("""
        Plan de collecte d'un produit : le produit de référence (nom, marque, GTIN, référence fabricant), les enseignes
        à interroger par priorité (enseignes favorites de sa typologie ou héritées de la typologie parente) et les
        instructions de collecte de la typologie, de la plus générale à la plus précise. À suivre pour chercher les
        offres du produit chez ces enseignes.
        """)]
    public static async Task<OfferCollectionRequest> GetCollectionPlan(
        ISender sender,
        [Description("Identifiant du produit.")] Guid productId,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetCollectionPlanQuery(productId), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "list_offers_pending_review", Title = "Offres à valider", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("""
        Offres proposées par la collecte qui attendent une validation humaine (rapprochement non confirmé par GTIN ou
        marque + référence fabricant), des plus anciennes aux plus récentes, avec le dernier prix relevé.
        """)]
    public static async Task<IReadOnlyCollection<PendingOfferResponse>> ListOffersPendingReview(
        ISender sender,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new GetOffersPendingReviewQuery(), cancellationToken)).ValueOrToolError();
}
