using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pricely.Common.Application.AI;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;
using Pricely.Modules.Pricing.Application.PageSnapshots;
using Pricely.Modules.Pricing.Application.PageSnapshots.FetchMerchantPage;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Infrastructure.Collection;

/// <summary>
/// The collection agent of Pricely (ADR-008, ADR-017): the model of <see cref="PricingAiFeatures.OfferCollection"/>
/// searches each merchant of the request on its own site (<c>fetch_merchant_page</c>) and gives back what it read
/// (<c>submit_offer</c>). A submitted price must appear in a page fetched during this run, and at most one offer is kept
/// per merchant. The agent never confirms an offer: the matching policy and the reviewers do.
/// </summary>
internal sealed class AgentOfferCollector(
    ILlmGateway llmGateway,
    MerchantPageReader pageReader,
    SubmittedOfferVerifier verifier,
    ILoggerFactory loggerFactory,
    IServiceProvider services)
    : IOfferCollector
{
    internal const string AgentName = "pricing-collector";

    private const int ToolCallsPerMerchant = 6;
    private const int MaxToolRoundTrips = 40;

    private static readonly Error AgentFailed = Error.Problem(
        "Collection.AgentFailed",
        "The collection agent failed before collecting any offer.");

    private readonly ILogger _logger = loggerFactory.CreateLogger<AgentOfferCollector>();

    public async Task<Result<IReadOnlyCollection<CollectedOffer>>> CollectAsync(
        OfferCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Merchants.Count == 0)
        {
            return Result.Success<IReadOnlyCollection<CollectedOffer>>([]);
        }

        var run = new CollectionRun(request, pageReader, verifier);
        LlmClient llm = await llmGateway.ForFeatureAsync(PricingAiFeatures.OfferCollection, cancellationToken);
        string sessionId = $"collection:{request.Product.ProductId}:{Guid.CreateVersion7()}";

        using var chatClient = new FunctionInvokingChatClient(llm.AsChatClient(sessionId, agentName: AgentName), loggerFactory, services)
        {
            MaximumIterationsPerRequest = Math.Min(ToolCallsPerMerchant * request.Merchants.Count + 2, MaxToolRoundTrips),
            IncludeDetailedErrors = false
        };

        var options = new ChatOptions { Instructions = CollectionPrompt.Instructions, Tools = run.Tools() };

        try
        {
            await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, CollectionPrompt.Task(request))], options, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "The collection agent failed for product {ProductId}", request.Product.ProductId);

            if (run.Collected.Count == 0)
            {
                return Result.Failure<IReadOnlyCollection<CollectedOffer>>(AgentFailed);
            }
        }

        return Result.Success<IReadOnlyCollection<CollectedOffer>>(run.Collected);
    }

    /// <summary>The tools of one collection, bound to its merchants and to the pages it fetched.</summary>
    internal sealed class CollectionRun(OfferCollectionRequest request, MerchantPageReader pageReader, SubmittedOfferVerifier verifier)
    {
        private readonly Dictionary<Guid, CollectedOffer> _offers = [];
        private readonly HashSet<Guid> _snapshots = [];
        private readonly HashSet<Guid> _merchants = request.Merchants.Select(m => m.MerchantId).ToHashSet();

        public IReadOnlyCollection<CollectedOffer> Collected => _offers.Values;

        public IList<AITool> Tools() =>
        [
            AIFunctionFactory.Create(
                FetchMerchantPageAsync,
                new AIFunctionFactoryOptions
                {
                    Name = "fetch_merchant_page",
                    Description = "Lit une page du site d'une enseigne du plan : une adresse de ce site, ou sa page de recherche pour un texte. Renvoie le texte de la page et l'identifiant de sa capture."
                }),
            AIFunctionFactory.Create(
                SubmitOfferAsync,
                new AIFunctionFactoryOptions
                {
                    Name = "submit_offer",
                    Description = "Transmet l'offre lue sur une page capturée par fetch_merchant_page : le prix doit figurer dans cette page. Une offre par enseigne (la dernière transmise remplace la précédente)."
                })
        ];

        public async Task<object> FetchMerchantPageAsync(
            [Description("Identifiant de l'enseigne (voir le plan).")] Guid merchantId,
            [Description("Adresse absolue d'une page du site de l'enseigne, ou vide pour chercher.")] string? address = null,
            [Description("Texte à chercher sur le site de l'enseigne, si l'adresse est vide.")] string? searchText = null,
            CancellationToken cancellationToken = default)
        {
            if (!_merchants.Contains(merchantId))
            {
                return ToolError(OfferErrors.MerchantNotInCollectionPlan(merchantId, request.Product.ProductId));
            }

            Uri? page = null;
            if (!string.IsNullOrWhiteSpace(address) && !Uri.TryCreate(address.Trim(), UriKind.Absolute, out page))
            {
                return ToolError(PageSnapshotErrors.NoAddress(merchantId));
            }

            Result<PageSnapshot> snapshot = await pageReader.ReadAsync(merchantId, page, searchText, cancellationToken);
            if (snapshot.IsFailure)
            {
                return ToolError(snapshot.Error);
            }

            _snapshots.Add(snapshot.Value.Id);

            return FetchedPageResponse.From(snapshot.Value);
        }

        public async Task<object> SubmitOfferAsync(
            [Description("L'offre lue : capture, prix, disponibilité et ce que la page montre du produit.")] SubmittedOffer offer,
            CancellationToken cancellationToken = default)
        {
            if (!_snapshots.Contains(offer.SnapshotId))
            {
                return ToolError(PageSnapshotErrors.NotFound(offer.SnapshotId));
            }

            Result<CollectedOffer> collected = await verifier.VerifyAsync(offer, cancellationToken);
            if (collected.IsFailure)
            {
                return ToolError(collected.Error);
            }

            _offers[collected.Value.MerchantId] = collected.Value;

            return new { accepted = true, merchantId = collected.Value.MerchantId };
        }

        private static object ToolError(Error error) => new { error = error.Code, message = error.Description };
    }

    internal static class CollectionPrompt
    {
        public const string Instructions = """
            Tu es l'agent de collecte de prix de Pricely. Pour chaque enseigne du plan, cherche le produit de référence sur
            le site de l'enseigne avec fetch_merchant_page (sa page de recherche si elle est configurée, sinon une adresse
            de son site), ouvre la page du produit, puis transmets ce qu'elle affiche avec submit_offer.

            Règles :
            - Ne lis que les sites des enseignes du plan ; n'invente jamais d'adresse hors de ces sites.
            - Le prix transmis doit être celui affiché sur la page capturée citée (snapshotId), toutes taxes comprises,
              pour le conditionnement du produit de référence. Un lot, un autre conditionnement ou un autre format n'est
              pas le même produit : ne le transmets pas, ou baisse la similarité et explique-le.
            - Recopie ce que la page montre (titre, GTIN / EAN, marque, référence fabricant, référence de l'enseigne) sans
              le corriger ; laisse vide ce qui n'est pas affiché.
            - Donne une similarité entre 0 et 1 et une justification courte : un humain la lira pour valider l'offre.
            - Si une enseigne ne vend pas le produit, passe à la suivante. Ne transmets rien plutôt qu'une offre douteuse.
            - Termine par un résumé d'une ligne par enseigne.
            """;

        public static string Task(OfferCollectionRequest request)
        {
            var task = new StringBuilder();
            ProductToCollect product = request.Product;

            task.AppendLine("Produit de référence :")
                .AppendLine(CultureInfo.InvariantCulture, $"- nom : {product.Name}")
                .AppendLine(CultureInfo.InvariantCulture, $"- marque : {product.Brand}")
                .AppendLine(CultureInfo.InvariantCulture, $"- GTIN : {product.Gtin ?? "inconnu"}")
                .AppendLine(CultureInfo.InvariantCulture, $"- référence fabricant : {product.ManufacturerReference ?? "inconnue"}")
                .AppendLine()
                .AppendLine("Enseignes à interroger, par priorité :");

            foreach (MerchantToSearch merchant in request.Merchants)
            {
                string search = merchant.SearchTemplate is null ? "pas de page de recherche configurée" : "page de recherche configurée";
                task.AppendLine(CultureInfo.InvariantCulture, $"- {merchant.Name} (merchantId {merchant.MerchantId}) : {merchant.Website} ({search})");
            }

            if (!string.IsNullOrWhiteSpace(request.Instructions))
            {
                task.AppendLine()
                    .AppendLine("Instructions de collecte de la typologie :")
                    .AppendLine(request.Instructions);
            }

            return task.ToString();
        }
    }
}
