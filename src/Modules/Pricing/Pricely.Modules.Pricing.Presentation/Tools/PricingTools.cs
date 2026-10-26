using System.ComponentModel;
using MediatR;
using ModelContextProtocol.Server;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Presentation.Mcp;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;
using Pricely.Modules.Pricing.Application.Offers.ConfirmOffer;
using Pricely.Modules.Pricing.Application.Offers.GetCollectionPlan;
using Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;
using Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;
using Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;
using Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;
using Pricely.Modules.Pricing.Application.Offers.RejectOffer;
using Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;
using Pricely.Modules.Pricing.Application.PageSnapshots.FetchMerchantPage;

namespace Pricely.Modules.Pricing.Presentation.Tools;

/// <summary>
/// Pricing tools for AI agents (MCP and internal agent). Write tools (<c>ReadOnly = false</c>) become, for the internal
/// agent, an action that a person approves before it runs (ADR-008, ADR-013); on <c>/mcp</c> they are listed only to a
/// token holding their scope, <c>pricely.write</c> or the <see cref="ToolScopeAttribute"/> one (ADR-016).
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
    public static async Task<CollectionPlanResponse> GetCollectionPlan(
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

    [McpServerTool(Name = "collect_product_offers", Title = "Collecter les offres", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("""
        Lance la collecte des offres d'un produit chez les enseignes de son plan de collecte (voir get_collection_plan) :
        crée ou met à jour les offres et enregistre de nouveaux relevés de prix. Les offres qui ne sont pas rapprochées
        par GTIN ou marque + référence fabricant restent à valider. Action d'écriture : elle n'est exécutée qu'après
        validation par l'utilisateur.
        """)]
    public static async Task<CollectProductOffersResponse> CollectProductOffers(
        ISender sender,
        [Description("Identifiant du produit.")] Guid productId,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new CollectProductOffersCommand(productId), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "fetch_merchant_page", Title = "Lire une page d'enseigne", ReadOnly = true, Idempotent = false, OpenWorld = true)]
    [Description("""
        Lit une page du site d'une enseigne suivie et en garde une capture : soit une adresse de ce site, soit sa page de
        recherche pour un texte (si l'enseigne en a une). Seul le site de l'enseigne est lu. Renvoie le texte de la page
        et l'identifiant de la capture (snapshotId), à citer avec submit_collected_offer.
        """)]
    public static async Task<FetchedPageResponse> FetchMerchantPage(
        ISender sender,
        [Description("Identifiant de l'enseigne (voir list_merchants ou get_collection_plan).")] Guid merchantId,
        [Description("Adresse absolue d'une page du site de l'enseigne ; vide pour utiliser la recherche.")] Uri? address = null,
        [Description("Texte à chercher sur le site de l'enseigne, si l'adresse est vide.")] string? searchText = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new FetchMerchantPageCommand(merchantId, address, searchText), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "submit_collected_offer", Title = "Transmettre une offre", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("""
        Enregistre l'offre d'un produit lue sur une page capturée par fetch_merchant_page : le prix doit figurer dans la
        capture et l'enseigne dans le plan de collecte du produit. L'offre est rapprochée comme toute offre collectée
        (confirmée seulement sur GTIN ou marque + référence fabricant identiques, sinon à valider) et le prix est relevé.
        Action d'écriture : elle n'est exécutée qu'après validation par l'utilisateur.
        """)]
    public static async Task<SubmittedOfferResponse> SubmitCollectedOffer(
        ISender sender,
        [Description("Identifiant du produit de référence.")] Guid productId,
        [Description("L'offre lue : capture, prix, devise, disponibilité et ce que la page montre du produit.")] SubmittedOffer offer,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new SubmitCollectedOfferCommand(productId, offer), cancellationToken)).ValueOrToolError();

    [McpServerTool(Name = "confirm_offer", Title = "Confirmer une offre", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [ToolScope(PricelyScopes.Review)]
    [Description("""
        Confirme qu'une offre à valider (voir list_offers_pending_review) est bien le produit de référence : ses prix
        entrent dans le comparatif. Action de revue : elle n'est exécutée qu'après validation par l'utilisateur.
        """)]
    public static async Task<string> ConfirmOffer(
        ISender sender,
        [Description("Identifiant de l'offre.")] Guid offerId,
        CancellationToken cancellationToken = default)
    {
        (await sender.Send(new ConfirmOfferCommand(offerId), cancellationToken)).ThrowIfToolError();

        return "Offre confirmée.";
    }

    [McpServerTool(Name = "reject_offer", Title = "Rejeter une offre", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [ToolScope(PricelyScopes.Review)]
    [Description("""
        Rejette une offre à valider (voir list_offers_pending_review) : ce n'est pas le produit de référence. Une offre
        rejetée n'accepte plus de relevés de prix. Action de revue : elle n'est exécutée qu'après validation par
        l'utilisateur.
        """)]
    public static async Task<string> RejectOffer(
        ISender sender,
        [Description("Identifiant de l'offre.")] Guid offerId,
        CancellationToken cancellationToken = default)
    {
        (await sender.Send(new RejectOfferCommand(offerId), cancellationToken)).ThrowIfToolError();

        return "Offre rejetée.";
    }
}
